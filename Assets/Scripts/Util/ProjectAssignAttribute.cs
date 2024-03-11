using UnityEngine;

[System.AttributeUsage(System.AttributeTargets.Field)]
public class ProjectAssignAttribute : System.Attribute 
{
    [SerializeField]
    public string Header { get; set; }

    [SerializeField]
    public bool Hide { get; set; }
}