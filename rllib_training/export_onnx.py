from __future__ import annotations
import os

from typing import Callable
import ray
import torch
from torch import nn
from ray.rllib.utils.typing import List
from ray.rllib.policy.policy import Policy
import numpy as np
from utils.export_dist_funcs import sample_multicategorical_action_distribution_deterministic, \
    sample_diagonal_gaussian_action_distribution
from utils.config_reader import read_config, read_yaml
from utils.env_helper import register_envs
from ray.rllib.algorithms.ppo import PPO

from patformer_env import PlatformerAgent

base_dir = "results\\"
# checkpoint_path = "<exp_series>\\<PPO>\\<run_name>\<checkpoint_xxxxxx>"
checkpoint_path = "PPO_2025-11-13_00-58-46/PPO_PlatformerAgent_86a39_00000_0_2025-11-13_00-58-46/checkpoint_000001"
# file_name = None  # specify path if checkpoint is trained on another build
file_name = "..\\builds\\win\\hedgehogs.exe"

opset_version = 15
version_number = 3
memory_size = 0
behaviour = "PlatformerAgent"

checkpoint_path = os.path.join(base_dir, checkpoint_path)
onnx_model_suffix = ".onnx"

policy_specs = PlatformerAgent.get_policy_configs_for_game(behaviour)[0]
policy_keys = list(policy_specs.keys())
policy_spec = policy_specs[policy_keys[0]]
obs_space = policy_spec.observation_space
# discrete_actions = policy_spec.action_space.nvec
n_cont_actions = policy_spec.action_space.shape[0]

# input_names = ["obs_0", "obs_1", "action_masks"]
input_names = ["obs_0", "obs_1", "recurrent_in"]
# output_names = ["discrete_actions", "version_number", "memory_size",
#                 "discrete_action_output_shape"]
output_names = ["continuous_actions", "recurrent_out", "version_number", "memory_size",
                "continuous_action_output_shape"]


class RLLibTorchModelWrapper(nn.Module):

    def __init__(self, original_model: nn.Module,
                 model_action_distribution_sampler: Callable) -> None:
        """
            Initialise the wrapper model.
        """
        super().__init__()
        self.original_model = original_model

        self.register_buffer("continuous_act_size_vector", torch.Tensor([int(n_cont_actions)]).cuda())
        self.continuous_act_size_vector: torch.Tensor

        self.register_buffer("version_number", torch.Tensor([version_number]).cuda())
        self.version_number: torch.Tensor

        self.register_buffer("memory_size_vector", torch.Tensor([self.original_model.cell_size * 2]).cuda())
        self.memory_size_vector: torch.Tensor

        # self.register_buffer("h_out", torch.Tensor([int(self.original_model.cell_size)]).cuda())
        # self.h_out: torch.Tensor
        # self.register_buffer("c_out", torch.Tensor([int(self.original_model.cell_size)]).cuda())
        # self.c_out: torch.Tensor

        # Set modified forward model and action distribution sampler from methods passed in.
        self.action_distribution_sampler = model_action_distribution_sampler

    def rllib_model_forward(self, inputs: torch.Tensor):
            sensor_features = inputs[0].float()
            vector_features = inputs[1].float()
            sensor_features = sensor_features.permute(0, 3, 1, 2)
            sensor_features = self.original_model._convs(sensor_features)

            flat_features = self.original_model.post_conv.forward(sensor_features)
            flat_features = torch.cat([flat_features, vector_features], dim=1)
            pre_lstm = self.original_model.pre_lstm.forward(flat_features)

            pre_lstm = pre_lstm.unsqueeze(1)
            cell_size = self.original_model.cell_size
            h = inputs[2][:, :cell_size].unsqueeze(0).contiguous()
            c = inputs[2][:, cell_size:].unsqueeze(0).contiguous()
            # h,c = torch.split(inputs[2], self.original_model.cell_size, dim=1)
            # h = h.unsqueeze(0)
            # c = c.unsqueeze(0)
            m_features, (h, c) = self.original_model.lstm(
                pre_lstm, [h, c])
            m_features = m_features.squeeze(1)
            logits, _ = self.original_model.fcnet({"obs": m_features}, None, None)
            memory = torch.cat([h.squeeze(0), c.squeeze(0)], dim=1)
            return logits, memory

    def forward(self, *inputs: List[torch.Tensor]):
        """
            Run a forward pass through the wrapped model.
        """
        # Get action prediction distributions from model.
        logits, memory = self.rllib_model_forward(inputs)
        # Sample actions from distributions.
        # mask = inputs[1] if len(inputs) > 1 else None
        # sampled_disc = self.action_distribution_sampler(logits, discrete_actions, mask)
        sampled_cont = self.action_distribution_sampler(logits)

        results = [sampled_cont, memory,
                   self.version_number, self.memory_size_vector,
                   self.continuous_act_size_vector]
        return tuple(results)


# def generate_sample_mask():
#     discrete_size = discrete_actions.sum()
#     return torch.ones([1, discrete_size]).cuda()


def get_sample_inputs_from_policy() -> List[torch.Tensor]:
    """
        Generate a batch of dummy data for use in model tracing with ONNX exporter.
    """
    test_data = obs_space.sample()
    test_data = [np.repeat(np.expand_dims(d, axis=0), 8, axis=0) for d in test_data]
    test_data = [torch.tensor(d).cuda() for d in test_data]
    return test_data


def export_onnx_model(model: nn.Module, sample_data: torch.Tensor, onnx_export_path: str,
                      opset_version: int, input_names: list, output_names: list, dynamic_axes: dict) -> None:
    """
        Export an torch.nn.Module model as an ONNX model.
    """
    # Export ONNX model from torch model.
    torch.onnx.export(
        model,
        sample_data,
        onnx_export_path,
        opset_version=opset_version,
        input_names=input_names,
        output_names=output_names,
        dynamic_axes=dynamic_axes,
    )


def export_onnx_model_from_rllib_checkpoint() -> None:
    """
        Export the RLLib model as an MLAgents-compatible ONNX model.
    """
    # Create checkpoint and ONNX model output paths.

    experiment_config = read_config(checkpoint_path)
    experiment_config['config']['num_rollout_workers'] = 0
    register_envs(experiment_config)

    algorithm = PPO(experiment_config['config'])
    algorithm.load_checkpoint(checkpoint_path)
    for policy_id in policy_keys:
        policy = algorithm.get_policy(policy_id)
        onnx_out_path = checkpoint_path + "_" + policy_id + onnx_model_suffix
        print("Exporting policy:", policy_id, "\n\tfrom:\n", checkpoint_path, "\n\tto:\n", onnx_out_path)

        # Get RLLib policy model from policy.
        model = policy.model
        # Choose sampling method according to action outputs for model
        # sampling_method = sample_multicategorical_action_distribution_deterministic
        sampling_method = sample_diagonal_gaussian_action_distribution
        # Get wrapped model.
        mlagents_model = RLLibTorchModelWrapper(model, sampling_method)
        # Get sample inputs for tracing model.
        sample_obs = get_sample_inputs_from_policy()
        sample_rnn = torch.zeros(8, 64, device="cuda")
        # sample_mask = generate_sample_mask()
        sample_inputs = (sample_obs[0], sample_obs[1], sample_rnn)
        # Don't need mask when only cont actions
        # sample_inputs = sample_obs
        # Create list of input names, output names, and make the appropriate axes dynamic.
        dynamic_axes = {name: {0: "batch"} for name in input_names}
        dynamic_axes.update({name: {0: "batch"} for name in output_names})
        dynamic_axes.pop("version_number")
        dynamic_axes.pop("memory_size")
        dynamic_axes.pop("continuous_action_output_shape")

        # Export ONNX model from RLLib checkpoint.
        export_onnx_model(mlagents_model, sample_inputs, onnx_out_path, opset_version, input_names,
                          output_names, dynamic_axes)


if __name__ == "__main__":
    # Init ray to start algorithm
    ray.init()
    export_onnx_model_from_rllib_checkpoint()
    print("export done")
    ray.shutdown()
