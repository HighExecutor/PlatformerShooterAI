from ray.rllib.models.torch.visionnet import VisionNetwork
from typing import Dict, List
import numpy as np
from typing import Dict, List
import gymnasium as gym
from gymnasium.spaces import Box

from ray.rllib.models.torch.torch_modelv2 import TorchModelV2
from ray.rllib.models.utils import get_activation_fn, get_filter_config
from ray.rllib.utils.annotations import override
from ray.rllib.utils.framework import try_import_torch
from ray.rllib.utils.typing import ModelConfigDict, TensorType
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
        if not model_config.get("conv_filters"):
            model_config["conv_filters"] = get_filter_config(obs_space.shape)

        TorchModelV2.__init__(
            self, obs_space, action_space, num_outputs, model_config, name
        )
        nn.Module.__init__(self)

        activation = self.model_config.get("conv_activation")
        filters = self.model_config["conv_filters"]

        self._logits = None

        layers = []
        (w, h, in_channels) = obs_space.shape

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
        in_size = [
            np.ceil(in_size[0] - kernel[0] + stride),
            np.ceil(in_size[1] - kernel[1] + stride),
        ]
        layers.append(nn.Flatten())
        in_size = int(out_channels * in_size[0] * in_size[1])

        self._convs = nn.Sequential(*layers)
        self.fcnet = FullyConnectedNetwork(obs_space=Box(float("-inf"), float("inf"),(in_size,)), action_space=action_space,
                                           num_outputs=num_outputs, model_config=self.model_config, name="fcnet")
        self._features = None


    @override(TorchModelV2)
    def forward(
            self,
            input_dict: Dict[str, TensorType],
            state: List[TensorType],
            seq_lens: TensorType,
    ) -> (TensorType, List[TensorType]):
        self._features = input_dict["obs"].float()
        # Permuate b/c data comes in as [B, dim, dim, channels]:
        self._features = self._features.permute(0, 3, 1, 2)
        conv_out = self._convs(self._features)
        logits, state = self.fcnet({"obs": conv_out}, state, seq_lens)
        return logits, state

    def value_function(self) -> TensorType:
        return self.fcnet.value_function()