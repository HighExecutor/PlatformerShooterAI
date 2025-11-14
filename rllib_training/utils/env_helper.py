from typing import Dict, Callable
from patformer_env import PlatformerAgent
from ray.rllib.env.wrappers.unity3d_env import Unity3DEnv


def register_envs(experiment_config):
    from ray import tune
    if experiment_config["config"]["env"] == "PlatformerAgent":
        env_class = PlatformerAgent
    else:
        env_class = Unity3DEnv
    tune.register_env(
        experiment_config["config"]["env"],
        lambda c: env_class(**experiment_config['config']['env_config'])
    )