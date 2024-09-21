# User Study Results and Setup
The experiment consisted of two parts, the parameter inference to obtain the parameters of the balancing agent which was then used for the training of the supervisor and the user study to measure human performance in 4 conditions.


## Training and Parameter inference
The Attention Management System (AMS) agent was trained based on a cognitive model defined by its parameters. They were determined by an inverse modeling approach, which describes the process of inferring parameters from behavioral data. Therefore, the behavioral data of a single participant with average gaming skills (value of 4 on a scale from 1 to 7, where 1 is "I Don't play at all" and 7 is "Professional Level") was collected by playing the game for $500000$ actions with a "random" AMS (i.e., random switching interval with a minimum of 0.2s) and a constant difficulty level (fixed ball drag value of 0.8). 2000 different parameter assignments were evaluated and compared to the human behavior via a distance function. The parameterization of the model most similar to the human behavior was used for training and evaluation of the AMSs (Standard Deviation $\sigma_P=.01$; Standard Deviation $\sigma_{mean}=.2$; Update Period $u=.4$; Observation Probability $o_p=.4$; Old Distribution Persistence Time $d_t=.1$). The balancing agent was trained for $10$ million steps in combination with the AMS agent which was trained for $5$ million steps. A step is defined by a single decision. The training of both agents converges after the corresponding steps. More technical details about parameter inference and the training process can be found in the supplementary materials.
The results can be found in the following Jupyter notebook utilizing the evaluation and distance functions provided in `evaluation.py` and `distances.py`:
```
Scores/EvaluationSession19.ipynb
```


## User Study
We evaluated the AMS prototypes in a lab experiment. Our main hypothesis addresses the question of whether participants supported by the AMS achieve higher performance than when they decide which tasks to attend at each moment. Further, we aimed to investigate if the AMS would work better when being trained on the user model with cognitive constraints, compared to the unconstrained model. Finally, we wanted to know how the AMS would perform when task switches are not automatically performed but indicated to the user via notifications. This yielded the following four conditions, which were tested in a quasi-randomized within-subjects experimental design:
* _Cognitive Model_ - In this condition, participants played the game with the AMS trained on the user model with cognitive constraints. The AMS automatically switched between the platforms based on the learned policy. 
* _Unconstrained_ - In this condition, participants played the game with the AMS trained on the unconstrained user model. Again, the AMS switched between the platforms based on the learned policy automatically. 
* _Notification_ - We wanted to know if the AMS would also benefit users when only notifying them. In this condition, the AMS used the policy trained using the _cognitive_ model. However, task switches were not enforced automatically but had to be confirmed by the user by pressing a button. To allow for suitable comparisons, we decided to automatically switch to the target platform in case the user does not confirm within one second.
* _No Supervisor_ - In this condition, no AMS was present and the task of switching between the platforms was solely performed by the participants. This condition acts as the baseline control condition for investigating the main hypothesis.

The results of the user study can be found in the following Jupyter notebook:
```
Scores/Result_experimentKW26.ipynb
```

The measured task-switching behavior can be found in the following Jupyter notebook:
```
Scores/Task-switching_Analysis.ipynb
```


### Monotask Study
To be able to compare the dual-task performance (i.e., playing on both and switching between the two platforms) with a monotask version of the game, we conducted a small follow-up experiment. In the monotask version, only one platform was active and participants had to balance the ball until the game was over (i.e, the ball fell off the platform). Since we made the game increasingly difficult over time (see section 3.1 in the main paper) the monotask version would end sooner or later. All game properties (i.e., ball speeds, user input, etc.) were exactly the same as in the dual-task study. 
We invited N=12 participants (10 male, 2 female; M=22, SD=1.68 years old, mainly bachelor students, average hours of gaming per week M=5.08, SD=5.05 hours) to play the monotask version of the game. Participants played the game until they finished two episodes longer than 5 seconds. We kept the 5-second threshold from the main study but restricted it to playing only two longer episodes as the monotask version is quite boring and easier to play. Thus, longer playing times had to be expected.

The results of the baseline experiment can be found in the following Jupyter notebook:
```
Scores/Result_experimentKW26_baseline.ipynb
```