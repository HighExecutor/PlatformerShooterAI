import argparse

import ray
from ray import tune
from ray.rllib.algorithms.ppo import PPO, PPOConfig
from patformer_env import PlatformerAgent
# from custom_visionnet import CustomVisionNetwork
from custom_lstm_visionnet import CustomVisionNetwork
from rollout_reward_callback import RolloutRewardTracker
# from curriculum import curriculum_config, curriculum_fn, CurriculumCallback, single_task, default_task

local_mode = False
ray.init(local_mode=local_mode)

args = argparse.Namespace
args.env = "PlatformerAgent"
args.file_name = "E:\Projects\hedgehogs\\builds\win\\hedgehogs.exe"
# args.file_name = None
result_dir = "E:\\Projects\\hedgehogs\\rllib_training\\results\\"
# args.file_name = None
args.from_checkpoint = None
args.stop_iters = 999999
args.stop_timesteps = 999999999
args.stop_reward = 9999.0
args.framework = "torch"
args.num_workers = 4 if not local_mode else 0
args.no_graphics = True
args.time_scale = 20
batch_size = 4048*2

policies, policy_mapping_fn = PlatformerAgent.get_policy_configs_for_game("PlatformerAgent")

tune.register_env(
    "PlatformerAgent",
    lambda c: PlatformerAgent(file_name=c["file_name"], no_graphics=c["no_graphics"],
                           curriculum_config=None, time_scale=c["time_scale"]),
)

config = (
    PPOConfig()
    .environment(
        env="PlatformerAgent",
        disable_env_checking=True,
        env_config={"file_name": args.file_name,
                    "no_graphics": args.no_graphics,
                    "time_scale": args.time_scale},
        # env_task_fn=curriculum_fn
    )
    .framework(args.framework)
    .rollouts(
        num_rollout_workers=args.num_workers if args.file_name and not local_mode else 0,
        # batch_mode="truncate_episodes",
        batch_mode="complete_episodes",
        rollout_fragment_length="auto",
    )
    .training(
        lr=0.0003,
        lambda_=0.95,
        gamma=0.99,
        sgd_minibatch_size=batch_size // 8,
        train_batch_size=batch_size,
        num_sgd_iter=8,
        vf_loss_coeff=1.0,
        clip_param=0.2,
        entropy_coeff=0.001,
        model={
            "fcnet_hiddens": [128],
            "vf_share_layers": True,
            "conv_filters": [[16, [3, 3], 1], [16, [3, 3], 1]],
            "custom_model": CustomVisionNetwork,
            "lstm_cell_size": 32,
        },
    )
    .multi_agent(policies=policies, policy_mapping_fn=policy_mapping_fn)
    .resources(num_gpus=1)
    .debugging(log_level="INFO")
    # .callbacks(RolloutRewardTracker)
    # .callbacks(CurriculumCallback)
)
stop = {
    "training_iteration": args.stop_iters,
    "timesteps_total": args.stop_timesteps,
    "episode_reward_mean": args.stop_reward,
}

tune.run(
    'PPO',
    config=config.to_dict(),
    stop=stop,
    verbose=3,
    checkpoint_freq=100,
    checkpoint_at_end=True,
    storage_path=result_dir,
    # restore="E:\wspace\\rl_tutorial\\rllib_results_nodrag\PPO_2025-02-21_10-09-39\PPO_SpaceScalEnv_94920_00000_0_2025-02-21_10-09-39\checkpoint_000028"
)

ray.shutdown()
