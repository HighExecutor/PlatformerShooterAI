from ray.rllib.models.torch.visionnet import VisionNetwork
from typing import Dict, List, Tuple
import numpy as np
from typing import Dict, List
import gymnasium as gym
from gymnasium.spaces import Box

from ray.rllib.models.torch.torch_modelv2 import TorchModelV2
from ray.rllib.models.utils import get_activation_fn, get_filter_config
from ray.rllib.utils.annotations import override
from ray.rllib.utils.framework import try_import_torch
from ray.rllib.utils.typing import ModelConfigDict, TensorType
from ray.rllib.policy.rnn_sequencing import add_time_dimension
from ray.rllib.models.torch.fcnet import FullyConnectedNetwork
from ray.rllib.models.torch.misc import (
    normc_initializer,
    same_padding,
    SlimConv2d,
    SlimFC,
)
torch, nn = try_import_torch()


class CustomVisionNetwork(VisionNetwork):
    def __init__(
        self,
        obs_space: gym.spaces.Space,
        action_space: gym.spaces.Space,
        num_outputs: int,
        model_config: ModelConfigDict,
        name: str,
    ):
        TorchModelV2.__init__(
            self, obs_space, action_space, num_outputs, model_config, name
        )
        nn.Module.__init__(self)

        vision_space = obs_space.original_space.spaces[0]
        vector_space = obs_space.original_space.spaces[1]

        activation = self.model_config.get("conv_activation")
        filters = self.model_config["conv_filters"]
        self.cell_size = self.model_config["lstm_cell_size"]
        self._logits = None

        layers = []
        (w, h, in_channels) = vision_space.shape

        in_size = [w, h]
        for out_channels, kernel, stride in filters[:-1]:
            padding, out_size = same_padding(in_size, kernel, stride)
            layers.append(
                SlimConv2d(
                    in_channels,
                    out_channels,
                    kernel,
                    stride,
                    padding,
                    activation_fn=activation,
                )
            )
            in_channels = out_channels
            in_size = out_size

        out_channels, kernel, stride = filters[-1]

        layers.append(
            SlimConv2d(
                in_channels,
                out_channels,
                kernel,
                stride,
                None,  # padding=valid
                activation_fn=activation,
            )
        )
        layers.append(nn.Flatten())
        self._convs = nn.Sequential(*layers)

        in_size = [
            np.ceil(in_size[0] - kernel[0] + stride),
            np.ceil(in_size[1] - kernel[1] + stride),
        ]
        in_size = int(out_channels * in_size[0] * in_size[1])

        self.post_conv = SlimFC(
            in_size=in_size,
            out_size=256,
            initializer=normc_initializer(1.0),
            activation_fn=activation,
        )

        self.pre_lstm = SlimFC(
            in_size=256 + vector_space.shape[0],
            out_size=self.cell_size,
            initializer=normc_initializer(1.0),
            activation_fn=activation,
        )

        self.lstm = nn.LSTM(
            self.cell_size, self.cell_size, batch_first=True,
        )

        self.fcnet = FullyConnectedNetwork(obs_space=Box(float("-inf"), float("inf"),(self.cell_size,)), action_space=action_space,
                                           num_outputs=num_outputs, model_config=self.model_config, name="fcnet")
        self._features = None

    @override(TorchModelV2)
    def forward(
            self,
            input_dict: Dict[str, TensorType],
            state: List[TensorType],
            seq_lens: TensorType,
    ) -> Tuple[TensorType, List[TensorType]]:
        self._features = input_dict["obs"][0].float()
        self._vector_features = input_dict["obs"][1].float()
        # Permuate b/c data comes in as [B, dim, dim, channels]:
        self._features = self._features.permute(0, 3, 1, 2)
        conv_out = self._convs(self._features)

        flat_features = self.post_conv.forward(conv_out)
        flat_features = torch.cat([flat_features, self._vector_features], dim=1)
        pre_lstm = self.pre_lstm.forward(flat_features)

        pre_lstm = add_time_dimension(
            pre_lstm,
            seq_lens=seq_lens,
            framework="torch",
            time_major=self.time_major,
        )

        m_features, [h, c] = self.lstm(
            pre_lstm, [torch.unsqueeze(state[0], 0), torch.unsqueeze(state[1], 0)]
        )
        m_features = torch.reshape(m_features, [-1, self.cell_size])
        logits, _ = self.fcnet({"obs": m_features}, state, seq_lens)
        return logits, [torch.squeeze(h, 0), torch.squeeze(c, 0)]

    def value_function(self) -> TensorType:
        return self.fcnet.value_function()

    def get_initial_state(self):
        linear = next(self.pre_lstm._model.children())
        h = [
            linear.weight.new(1, self.cell_size).zero_().squeeze(0),
            linear.weight.new(1, self.cell_size).zero_().squeeze(0),
        ]
        return h