using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITaskSettings
{
    public int decisionPeriod { get; set; }

    public string baseClassName { get; set; }
}
