# Model Training and Evaluation
The project includes two pipelines to train and evaluate the agents. The pipelines allow to specify different training and evaluation scenarios to train/evaluate multiple environment settings in parallel with the help of configuration files. 


## Model Format
The training pipeline saves the trained model as a scriptable object (`asset` extension) instead of the standard _ONNX_ format to include additional information:
* _ONNX_ model - The trained model.
* Supervisor Settings - The used supervisor/environment settings during the training like if a random supervisor was used or the global starting drag value of the balls. This information is relevant since it could be that the model only works for a configuration that is similar to the used settings during the training.
* Type - Class name of the trained agent. This information is used in the _ProjectSettings_ inspector to automatically assign the specified model to the fitting agent.
* Decision Period - The used decision period of the agent during the training.


## Training
Change the directory to `config`. Here you can find the script `train.py` that is responsible for the training of a single model and `run_trainings.py` that allows to configure the training of multiple models.


### train.py
The following command starts the training:

```
train.py [-h] [--num_envs NUM_ENVS] model_config_file environment_config_file session_dir
```

This command trains the model and saves the result to `session_dir`. The function takes the following parameters:
* `model_config_file` - Config file in _YAML_ format of the hyperparameter for model training (see [Behavior-Configurations](https://github.com/Unity-Technologies/ml-agents/blob/develop/docs/Training-ML-Agents.md#behavior-configurations)).
* `environment_config_file` - Config file in _JSON_ format consisting of the different parameters.
* `session_dir` - Directory where the models are saved.

Example `environment_config_file` used for the training of the CR and supervisor model:

```
{
	"hyperparameters": {
		"autonomous": false,
		"decisionPeriod": 4,
		"focusActivePlatform": false,
		"resetPlatformToIdentity": true,
		"agentChoice": "Ball3DAgentHumanCognition",
		"trainSupervisor": true,
		"trainBallAgent": true,
		"supervisorModelName": "",
		"ballAgentModelName": ""
	},
	"supervisorSettings": {
		"randomSupervisor": false,
		"vectorObservationSize": 18,
		"setConstantDecisionRequestInterval": true,
		"decisionRequestIntervalInSeconds": 0.2,
		"decisionRequestIntervalRangeInSeconds": 0,
		"globalDrag": 0.8,
		"useNegativeDragDifficulty": true,
		"difficultyIncrementInterval": 5,
		"decisionPeriod": 5,
		"advanceNoticeInSeconds": 0.3,
		"ballAgentDifficulty": 170,
		"ballAgentDifficultyDivisionFactor": 1.05
	},
	"ball3DAgentHumanCognitionSettings": {
		"numberOfBins": 1000,
		"showBeliefState": false,
		"numberOfSamples": 1000,
		"sigma": 0.01,
		"sigmaMean": 0.2,
		"updatePeriode": 0.4,
		"observationProbability": 0.1,
		"constantReactionTime": 0,
		"oldDistributionPersistenceTime": 0.4,
		"fullVision": false
	}
}
```
You can configure your components directly in this file, as outlined in the following section.

### Settings Definition
When using vanilla ML-Agents, changing your environment (e.g., the parameters of CR-agents) requires manual adjustments in the Unity editor, which can be error-prone as it’s easy to overlook certain configurations. The toolkit simplifies this process by enabling component configuration without additional implementation. It automatically checks the provided configuration files for your components (e.g., tasks) and uses reflection to assign the specified values directly to the corresponding fields in your class. All that’s needed is to assign the `[field: SerializeField]` attribute to the relevant field. After that, you can configure your component using its name, along with the associated fields and values. An example configuration for the typing agent might look like this:

```
{
	
	...

	"typingAgentHumanCognition": {
		"quizName": "quiztest.csv",
		"screenWidthPixel": 1920,
		"screenHightPixel": 1080,
		"screenDiagonalInch": 27,
		"showBeliefState": true,
		"fullVision": true,
		"numberOfSamples": 99,
		"observationProbability": 0.5
	},
}
```


### run_trainings.py
The following command starts the trainings:

```
run_trainings.py [-h] [--number_of_environments NUMBER_OF_ENVIRONMENTS] [--verbose] model_config_file environment_config_list_file 
```

The function takes the following parameters that were not already described:
* `environment_config_list_file` - Config file in _JSON_ format consisting of lists of the different parameters to generate the environment config files.
* `number_of_environments` - If this value is passed, only a subset of the possible parameter combinations is generated. The subset consists of combinations with a maximal distance.
* `verbose` - Prints the iterated distance table. Therefore a higher distance is indicating that the value was added later. The first added value is marked with a distance of -1.

The script will generate the necessary configuration files based on the `environment_config_list_file` file and then start the training process for each. The `environment_config_list_file` file could like that:

```
{
	
	...

	"ball3DAgentHumanCognitionSettings": {
		"useFocusAgent": [false]
		"numberOfBins": [1000],
		"showBeliefState": [false],
		"numberOfSamples": [1000],
		"sigma": [0.05, 0.1, 0.2, 0.5],
		"sigmaMean": [0.05, 0.1, 0.2, 0.5],
		"updatePeriode": [0.1, 0.2, 0.3],
		"observationProbability": [0.001, 0.005, 0.01],
		"constantReactionTime": [0],
		"oldDistributionPersistenceTime": [0, 0.1, 0.2, 0.3]
    }
} 
```

This config file would result in 576 different training configurations.


## Evaluation
The evaluation returns the behavior measurement of the specified agent configurations to the `Scores\Evaluations` directory. This behavior can then be compared to human behavior by using a distance metric (see [User Study: Results and Setup](User-Study-Results-and-Setup.md) how this was done for this paper). The `evaluation_config_file` file contains the settings for the evaluation. For instance: 

```
{

  ...

  "behavioralDataCollectionSettings": {
	"collectDataForComparison": true,
	"maxNumberOfActions": 300000,
    	"numberOfAreaBins_BehavioralData": 196,
    	"numberOfBallVelocityBinsPerAxis_BehavioralData": 5,
	"numberOfAngleBinsPerAxis": 4,
    	"numberOfDistanceBins": 12,
    	"numberOfDistanceBins_velocity": 12,
	"numberOfActionBinsPerAxis": 5,
	"numberOfTimeBins": 5
    } 
}
```

The `maxNumberOfActions` parameter determines when the measurement process concludes, specifying the number of actions performed. It's crucial to set this value to match the number of actions taken during the measurement of the human participant for accurate comparison. The other values specify how the state space should be discretized to then compare the individual values of the bins between the agent and human behavior.

The following command starts the evaluation:

```
usage: run_evaluations.py [-h] [--environment_config_list_file ENVIRONMENT_CONFIG_LIST_FILE] [--number_of_environments NUMBER_OF_ENVIRONMENTS] [--nobuild NOBUILD] [--start_index START_INDEX] [--copy_raw_data] [--target_dir TARGET_DIR] ball_agent_models_dir_name evaluation_config_file [comparison_file_name]
```

The function takes the following parameters:
* `ball_agent_models_dir_name` - Name of models directory in Assets/Models path which should be evaluated.
* `evaluation_config_file` - _JSON_ config file with the evaluation parameters.
* `comparison_file_name` - Data based on which the models should be compared/evaluated.
* `environment_config_list_file` - Config file in _JSON_ format consisting of lists of the different parameters to generate the environment config files. Replaces config files of `ball_agent_models_dir_name` if given.
* `number_of_environments` - If this value is passed, only a subset of the possible parameter combinations is generated. The subset consists of combinations with a maximal distance. Will be ignored if `environment_config_list_file` is not given.
* `nobuild` - The given directory name is used for the evaluation. The environments are not build.
* `start_index` - Is ignored if `number_of_environments` is not given. Returns the environments with the highest distance at starting index `Start_index`. For instance if `number_of_environments = 10` and `start_index = 5` then the first 5 environments with the largest distance are ignored and the next 10 environments are returned.
* `copy_raw_data` - If given also the raw data is copied to the Scores directory. This data can then be converted to the preferred discretization.
* `target_dir` - If given the results are saved to the target- instead of the session directory.


## Next Step
The last stage involves comparing the behavioral measurements through distance calculation. The [User Study: Results and Setup](User-Study-Results-and-Setup.md) page demonstrates how this was done for ["Supporting Task Switching with Reinforcement Learning"](https://dl.acm.org/doi/10.1145/3613904.3642063).
