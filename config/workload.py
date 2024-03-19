import psutil
import numpy as np

def is_cpu_overloaded(interval):
    if psutil.cpu_percent(interval=interval) > 80:
        return True
    else:
        return False
    
def get_number_of_environments_for_workload():
    number_cpu_instances = int(psutil.cpu_count(logical=True)*((100 - psutil.cpu_percent(interval=5))/100)*0.65)
    number_memory_instances = int((psutil.virtual_memory().available/(1024.0 ** 2))/280)-2

    print("Run {} instances in parallel (max possible instances --> cpu: {}, memory: {}).".format(min(number_cpu_instances, number_memory_instances), number_cpu_instances, number_memory_instances))

    return min(number_cpu_instances, number_memory_instances)