import psutil
import numpy as np

def is_cpu_overloaded(interval):
    if psutil.cpu_percent(interval=interval) > 80:
        return True
    else:
        return False
    
def get_number_of_environments_for_cpu_workload():
    return int(psutil.cpu_count(logical=True)*((100 - psutil.cpu_percent(interval=5))/100)*0.75)