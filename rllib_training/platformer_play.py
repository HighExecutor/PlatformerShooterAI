import argparse
import os

import ray
from ray import air, tune
from ray.rllib.algorithms.ppo import PPO, PPOConfig
from patformer_env import PlatformerAgent
from utils.config_reader import read_config
import numpy as np

try:
    ray.shutdown()
except:
    print("ray shutdown unnecessary")

ray.init(local_mode=True)

env = "PlatformerAgent"
file_name = "..\\builds\win\\hedgehogs.exe"

tune.register_env(
    "PlatformerAgent",
    lambda c: PlatformerAgent(file_name=c["file_name"], no_graphics=c["no_graphics"]),
)

base_dir = "results\\"
# checkpoint_path = "<exp_series>\\<PPO>\\<run_name>\<checkpoint_xxxxxx>"
checkpoint_path = "PPO_2025-11-13_00-58-46/PPO_PlatformerAgent_86a39_00000_0_2025-11-13_00-58-46/checkpoint_000000"
checkpoint_path = os.path.join(base_dir, checkpoint_path)

exp_config = read_config(checkpoint_path)['config']
exp_config['num_rollout_workers'] = 0
exp_config['env_config']["no_graphics"] = True
if file_name:
    exp_config['env_config']['file_name'] = file_name
agent = PPO(exp_config)
agent.load_checkpoint(checkpoint_path)
policy_name = PlatformerAgent.get_policy_name()
policies, p_map = PlatformerAgent.get_policy_configs_for_game("PlatformerAgent")


env = PlatformerAgent(file_name=file_name, no_graphics=False, time_scale=1, port=7001)
for _ in range(1000):
    score = np.zeros(4)
    state, info = env.reset()
    rnn_state = dict()
    for k in state.keys():
        policy_name = p_map(k, None, None)
        rnn_state[k] = agent.get_policy(policy_name).get_initial_state()
    for t in range(1000):
        actions = dict()
        for k, v in state.items():
            policy_name = p_map(k, None, None)
            actions[k], rnn_state[k], _ = agent.compute_single_action(observation=v, state=rnn_state[k],
                                                     policy_id=policy_name, unsquash_actions=False)
        s, r, d, _, i = env.step(actions)
        state = s
        score += np.resize(np.array(list(r.values())), 4)
        if True in d.values():
            break
    print("Score: " + str(score))
ray.shutdown()