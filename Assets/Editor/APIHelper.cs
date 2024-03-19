using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class APIHelper
{
    public static ICommandLineInterface CommandLineInterface { get; set; } = new CommandInterface();

    public static List<string> GetArgs()
    {
        string[] args = CommandLineInterface.GetCommandLineArgs();
        List<string> argsList = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-executeMethod")
            {
                for (int j = i + 2; j < args.Length; j++)
                { 
                    argsList.Add(args[j]);
                }

                break;
            }
        }

        return argsList;
    }
}


//Needed for mocking in test cases
public interface ICommandLineInterface
{
    string[] GetCommandLineArgs();
}


public class CommandInterface : ICommandLineInterface
{
    public string[] GetCommandLineArgs()
    {
        return Environment.GetCommandLineArgs();
    }
}