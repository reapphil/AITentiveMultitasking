import os, sys
import numpy as np
from pathlib import Path
import time
import elfi
from elfi.store import OutputPool
import scipy.stats as ss
import subprocess
import scipy
from observationUtils import getDurations, getSuspendedReactionTimeCounts, getNumberOfPerformedActionsPerEpisode, getActions
from workload import get_number_of_environments_for_cpu_workload, is_cpu_overloaded
import argparse
import warnings
from unity_helper import call_function
from datetime import datetime
import matplotlib.pyplot as plt
import pandas as pd
import pprint

import logging
logger = logging.getLogger()
logger.setLevel(logging.DEBUG)


np.seterr(divide='ignore', invalid='ignore')

sys.path.append(os.path.abspath(os.path.join('..', "Scores")))
import evaluation as ev
import evaluation_3d as ev_3d
import distances_3d as d_3d
import util as u

from normalizer import min_max_norm, min_max_norm_dict


warnings.filterwarnings("ignore", category=np.VisibleDeprecationWarning)

NUM_ENVS = 16
c = 0
n_episodes = 300
n_simulations = 300
n_evidence = 300
simulations_count = 0
is_bin_data = False

duration_actions_factor = 200
duration_supended_measuruement_factor = 2

# Set an arbitrary global seed to keep the randomly generated quantities the same
seed = 1
np.random.seed(seed)


def Main():
    parser = argparse.ArgumentParser()
    parser.add_argument("reactiontime_file", help="File containing the measured reactiontimes and the suspended reaction time counts.", type=str)
    parser.add_argument("duration_file", help="File containing the duration data.", type=str)
    parser.add_argument("behavior_file", help="File containing the behavior data.", type=str)
    parser.add_argument("environment_config_file", help="Config file in JSON format consisting of the different parameters", type=str)
    parser.add_argument("n_evidence", help="Number of evidence.", type=int, default=300)
    parser.add_argument("n_simulations", help="Number of episodes to simulate.", type=int, default=300)
    parser.add_argument("--use_fixed_number_of_measurments", "-f", help="If given only a fixed number of measurements is used for the inference based on the n parameter.", action='store_true')
    parser.add_argument("--load_model", "-l", help="Loads the model of the path of the given pickle file.", type=str)
    parser.add_argument("--method", "-m", help="Method of inference: BOLFI or Rejection sampling", type=str, default="BOLFI")
    parser.add_argument("--verbose", "-v", help="If given prints sampling results for BOLFI method.", action='store_true')

    args = parser.parse_args()


    code = call_function("BuildScript.BuildAbcEnvironment",
        Path("config", args.environment_config_file),
        logFile=Path("..", "Logs", "LogFileBuild.txt"))

    if code != 0:
        return
        
    global n_episodes
    global n_simulations
    global n_evidence
    global is_bin_data 
    global data_name
    global f

    if args.load_model is not None:
        data_name = args.load_model[:14]
    else:
        data_name = datetime.now().strftime("%Y%m%d%H%M%S")

    ch = logging.StreamHandler()
    ch.setLevel(logging.INFO)
    logger.addHandler(ch)

    fh = logging.FileHandler(Path("logs", data_name + ".log"))
    fh.setLevel(logging.DEBUG)
    logger.addHandler(fh)

    #f = open(data_name + ".txt", "a")
    
    is_bin_data = is_bin_data(args.reactiontime_file)

    n_simulations = args.n_simulations
    n_evidence = args.n_evidence

    if args.use_fixed_number_of_measurments:
        n_episodes = args.n_simulations
    else:
        n_episodes = 0

    scorePath = Path(os.getcwd(), "..", "Scores")

    sigma = elfi.Prior('uniform', 0, 2)
    sigmaMean = elfi.Prior('uniform', 0, 2) 
    updatePeriode = elfi.Prior('uniform', 0.02, 0.98) 
    observationProbability = elfi.Prior('uniform', 0.001, 0.099)
    constantReactionTime = elfi.Prior('uniform', 0, 1) 
    oldDistributionPersistenceTime = elfi.Prior('uniform', 0, 1)
    decisionPeriodBallAgent = elfi.Prior('uniform', 1, 9)
    

    #Load observations from disk
    if is_bin_data:
        observations = ev.getEvaluationResult(Path(scorePath, args.reactiontime_file), Path(scorePath, args.behavior_file), Path(scorePath, args.duration_file), getData=ev_3d.getNumpyArrays, getDataReactionTime=ev_3d.getNumpyArraysInclSuspendedCount)
    else:
        x, z = getActions(Path(scorePath, args.behavior_file), n_episodes*duration_actions_factor)
        observations = np.array([getSuspendedReactionTimeCounts(Path(scorePath, args.reactiontime_file), n_episodes*duration_supended_measuruement_factor), getNumberOfPerformedActionsPerEpisode(Path(scorePath, args.duration_file), n_episodes), x, z], dtype=object)
    #print(observations)

    # Add the simulator node and observed data to the model
    sim = elfi.Simulator(simulator, sigma, sigmaMean, updatePeriode, observationProbability, constantReactionTime, oldDistributionPersistenceTime, decisionPeriodBallAgent, observed=observations)

    if is_bin_data:
        d = elfi.Distance(bin_distance, sim)
    else:
        # Add summary statistics to the model -> returns [[S_m1 S_m2]] | S_m1 = mean(suspendedReactionTimeCounts), S_m2 = mean(numberOfPerformedActionsPerEpisode)
        # See https://elfi.readthedocs.io/en/latest/usage/adaptive_distance.html for a similar example
        S1 = elfi.Summary(mean, sim)
        S2 = elfi.Summary(std, sim)

        # Specify distance as euclidean between summary vector S1 from simulated and observed data (therefore for euclidean: sqrt((S_m1 - O_m1))^2 + (S_m2 - O_m2))^2))
        d = elfi.Distance(distance, S1, S2)

    if args.method.lower() == "bolfi":
        result = bolfi(d, args.load_model, args.verbose)
    elif args.method.lower() == "rejection":
        result = rejection(d, args.load_model)
    else:
        sys.exit("Method {} not implemented.".format(args.method))
    
    if result is not None:
        print(result)
        result.plot_marginals()
        plt.show()

    #f.close()


def is_bin_data(path):
    if Path(path).suffix == '.json':
        return True
    else:
        return False


def bolfi(d, model_path, isVerbose):
    log_d = elfi.Operation(np.log, d)

    bounds = {'sigma':(0.00001, 2), 'sigmaMean':(0.00001, 2), 'updatePeriode':(0.02, 1), 'observationProbability':(0.001, 0.1), 'constantReactionTime':(0, 1), 'oldDistributionPersistenceTime':(0, 1), 'decisionPeriodBallAgent': (1, 10)}
    acq_noise_var={'sigma':0.1, 'sigmaMean':0.1, 'updatePeriode':0.1, 'observationProbability':0.1, 'constantReactionTime':0.1, 'oldDistributionPersistenceTime':0.1, 'decisionPeriodBallAgent': 0}

    bolfi = elfi.BOLFI(log_d, 
            batch_size=1,
            bounds=bounds,
            acq_noise_var=acq_noise_var, 
            seed=seed,
            pool=get_pool(log_d, model_path))

    try:
        bolfi.fit(n_evidence=n_evidence)
    except KeyboardInterrupt:
        print("\naborting...")
        bolfi.pool.name = bolfi.pool.name[:14] + get_option_string(bolfi.n_evidence)

    bolfi.pool.save()

    print(pprint.pformat(bolfi.extract_result().x_min))

    #bolfi.plot_state()
    #bolfi.plot_discrepancy()
    #plt.show()

    if isVerbose:
        result = bolfi.sample(1000)#, algorithm='metropolis')

        return result


def rejection(d, model_path):
    pool = get_pool(d, model_path)
    n = get_number_of_environments_for_cpu_workload()-1
    print("Starting rejection sampling with batch size of {}.".format(n))

    rej = elfi.Rejection(d, 
                         batch_size= n if pool.batch_size is None else pool.batch_size, 
                         seed=seed,
                         pool=pool)
    
    try:
        result = rej.sample(n_evidence, threshold=.5)
    except KeyboardInterrupt:
        print("\naborting...")
        rej.pool.name = rej.pool.name[:14] + get_option_string(simulations_count)
        rej.pool.save()

        sys.exit("Evaluation aborted after {} simulations.".format(simulations_count))

    rej.pool.name = rej.pool.name[:14] + get_option_string(simulations_count)

    rej.pool.save()
    
    return result


def get_pool(target_node, model_path = None):
    option_string = get_option_string(n_evidence)

    if model_path is not None:
        pool = elfi.OutputPool.open(model_path)
        pool.name = pool.name[:14] + option_string
        return pool

    return OutputPool(["sim"] + target_node.model.parameter_names, name= data_name + option_string)


def get_option_string(n_evidence):
    return "{}ns{}".format(n_evidence, n_simulations)


def distance(X, Y):
    #print("X: " + str(X))
    #print("Y: " + str(Y))

    d = scipy.spatial.distance.cdist(X, Y, metric='euclidean')
    #print("Distance: " + str(d))

    return d


def bin_distance(X, Y):
    d = d_3d.distance_bin_data(X, Y)
    #f.write("Distance: {}\n".format(d))

    return d


#TODO: fix normalization to min_max_norm over both, the observation and the simulation data for distribution data.
def mean(y):
    y_norm = u.map_recursive(y, min_max_norm)

    return np.vectorize(np.mean)(y_norm)


def std(y):
    y_norm = u.map_recursive(y, min_max_norm)

    return np.vectorize(np.std)(y_norm)


def simulator(sigma, sigmaMean, updatePeriode, observationProbability, constantReactionTime, oldDistributionPersistanceTime, decisionPeriodBallAgent, batch_size = 1, random_state=None):
    """
    :param batch_size: defines how many different parameters are passed per call (e.g batch_size = 2: sigma = [x, y], sigmaMean = [x, y]... results in two calls with parameter combination of [x, x,...] and [y, y,...])
    """
    parameters = __build_para_combination_list(sigma, sigmaMean, updatePeriode, observationProbability, constantReactionTime, oldDistributionPersistanceTime, decisionPeriodBallAgent, batch_size)
    resultList = np.empty([batch_size, 4], dtype=object) if not is_bin_data else np.zeros(batch_size, dtype=object)

    procs = {}
    active_instances = 0

    for id, parameterList in enumerate(parameters):
        command = ["SupervisorML.exe", "-simulation", str(n_simulations), "-sigma", str(parameterList[0]), "-sigmaMean", str(parameterList[1]), "-updatePeriode", str(parameterList[2]), "-observationProbability", str(parameterList[3]), "-constantReactionTime", str(parameterList[4]), "-oldDistributionPersistenceTime", str(parameterList[5]), "-decisionPeriodBallAgent", str(int(parameterList[6])), "-id", str(id)]

        buildPath = Path("..", "Build", "abcSimulation")

        #print("Input: " + str(parameterList))
        logger.debug("Input: {}\n".format(parameterList))
        #f.write("Input: {}\n".format(parameterList))

        procs[id] = subprocess.Popen(
                        command, 
                        text=True, 
                        shell=True,
                        stdout=sys.stdout,
                        cwd=str(buildPath))
        active_instances += 1

        global simulations_count
        simulations_count += 1

        while is_cpu_overloaded(5):
            active_instances = fetchResults(procs, resultList, active_instances)


    while procs:
        fetchResults(procs, resultList, active_instances)

    #print("Output: " + str(resultList))
    
    return resultList


def fetchResults(procs, resultList, active_instances):
    buildPath = Path("..", "Build", "abcSimulation")

    for id, p in procs.copy().items():
        if p.poll() != None:
            active_instances -= 1
            procs.pop(id)

            try:
                if is_bin_data:
                    resultReactionTimePath = Path(buildPath, "SupervisorML_Data", "Scores", "{0}rt_sim.json".format(id))
                    resultDurationPath = Path(buildPath, "SupervisorML_Data", "Scores", "{0}sim_scores.csv".format(id))
                    resultActionPath = Path(buildPath, "SupervisorML_Data", "Scores", "{0}behavior_sim.json".format(id))

                    resultList[id] = ev.getEvaluationResult(resultReactionTimePath, resultActionPath, resultDurationPath, getData=ev_3d.getNumpyArrays, getDataReactionTime=ev_3d.getNumpyArraysInclSuspendedCount)
                else:
                    resultReactionTimePath = Path(buildPath, "SupervisorML_Data", "Scores", "{0}sim.csv".format(id))
                    resultDurationPath = Path(buildPath, "SupervisorML_Data", "Scores", "{0}switching_sim_scores.csv".format(id))
                    resultActionPath = Path(buildPath, "SupervisorML_Data", "Scores", "{0}raw_sim.csv".format(id))

                    suspendedReactionTimeCounts = getSuspendedReactionTimeCounts(resultReactionTimePath, n_episodes*duration_supended_measuruement_factor)
                    numberOfPerformedActionsPerEpisode = getNumberOfPerformedActionsPerEpisode(resultDurationPath, n_episodes)
                    performedActionsX, performedActionsZ = getActions(resultActionPath, n_episodes*duration_actions_factor)

                    #print("suspendedReactionTimeCounts: " + str(suspendedReactionTimeCounts))

                    resultList[id] = np.array([suspendedReactionTimeCounts, numberOfPerformedActionsPerEpisode, performedActionsX, performedActionsZ], dtype=object)
            except ValueError as e:
                print("Error: {} \nSuspendedReactionTimeCounts shape: {} \nNumberOfPerformedActionsPerEpisode shape: {} \nperformedActionsX shape: {} \nperformedActionsZ shape: {} \nRerun simulation...".format(e, np.shape(suspendedReactionTimeCounts), np.shape(numberOfPerformedActionsPerEpisode), np.shape(performedActionsX), np.shape(performedActionsZ)))
                
                procs[id] = subprocess.Popen(
                        p.args, 
                        text=True, 
                        shell=True,
                        stdout=sys.stdout,
                        cwd=str(buildPath))
                
                active_instances += 1

            #print("resultList[id]: " + str(resultList[id]))

    return active_instances


def __build_para_combination_list(sigma, sigmaMean, updatePeriode, observationProbability, constantReactionTime, oldDistributionPersistanceTime, decisionPeriodBallAgent, batch_size):
        parameters = []

        for i in range(batch_size):
             parameters.append([sigma[i], sigmaMean[i], updatePeriode[i], observationProbability[i], constantReactionTime[i], oldDistributionPersistanceTime[i], decisionPeriodBallAgent[i]])

        return parameters


if __name__ == '__main__':
    Main()