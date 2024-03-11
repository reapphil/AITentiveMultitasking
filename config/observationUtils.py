import pandas as pd
import numpy as np


def getDurations(filePath, N=0):
    df = pd.read_csv(filePath)

    result = df['Duration'].to_list()

    if N==0:
        return result
    else:
        return result[:N]


def getSuspendedReactionTimeCounts(filePath, N=0):
    df = pd.read_csv(filePath)

    result = df['SuspendedReactionTimeCount'].to_list()

    if N==0:
        return result
    else:
        return result[:N]


def getActions(filePath, N=0):
    df = pd.read_csv(filePath)

    x = df['ActionX'].to_list()
    z = df['ActionZ'].to_list()

    if N==0:
        return x, z
    else:
        return x[:N], z[:N]


def getNumberOfPerformedActionsPerEpisode(filePath, N=0):
    df = pd.read_csv(filePath)
    result = df.groupby(['EpisodeId', 'DateTime']).count()['PlayerName'].to_list()

    if N==0:
        return result
    else:
        return result[:N]

    