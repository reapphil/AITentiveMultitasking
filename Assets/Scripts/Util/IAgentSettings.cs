using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAgentSettings : ISettings
{
    public int? decisionPeriod { get; set; }

    public string baseClassName { get; set; }
}
