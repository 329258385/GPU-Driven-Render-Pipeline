﻿using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System;






public sealed class LoadingThread
{
    private struct Command
    {
        public object obj;
        public Action<object> func;
    }

    public static LoadingCommandQueue   commandQueue { get; private set; }
    public static LoadingThread         current { get; private set; }
    private static List<Command>        commands = new List<Command>();
    private List<Command>               localCommands = new List<Command>();
    private AutoResetEvent              resetEvent;
    private Thread                      thread;
    private bool                        isRunning;

    public void Init()
    {
        if (current != null)
        {
            Debug.LogError("Loading Thread should be singleton!");
            return;
        }
        current = this;
        isRunning = true;
        resetEvent = new AutoResetEvent(false);
        thread = new Thread(Run);
        thread.Start();
        commandQueue = new LoadingCommandQueue();
    }

    public static void AddCommand(Action<object> act, object obj)
    {
        lock (commands)
        {
            commands.Add(new Command
            {
                obj = obj,
                func = act
            });
            current.resetEvent.Set();
        }
    }

    private void Run()
    {
        while (isRunning)
        {
            resetEvent.WaitOne();
            lock (commands)
            {
                localCommands.AddRange(commands);
                commands.Clear();
            }
            foreach (var i in localCommands)
            {
                i.func(i.obj);
            }
            localCommands.Clear();
        }
    }

    public void Update()
    {
        lock (commandQueue)
        {
            commandQueue.Run();
        }
    }

    public void Dispose()
    {
        current = null;
        isRunning = false;
        resetEvent.Set();
        thread.Join();
        resetEvent.Dispose();
    }
}
