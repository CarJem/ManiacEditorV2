﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace ManiacEDv2.Actions
{
    class ActionsGroup : IAction
    {
        private bool closed;

        private List<IAction> actions = new List<IAction>();

        public bool IsClosed
        {
            get
            {
                return closed;
            }
        }

        public string Description => GetDiscription();

        public string GetDiscription()
        {
            return $"{actions.Count} actions";
            /*
            string disc = "";
            foreach (var action in actions)
            {
                disc += action.Description + Environment.NewLine;
            }
            return disc;*/
        }

        public ActionsGroup()
        {
        }

        public void AddAction(IAction action)
        {
            if (closed) throw new Exception("Can't add action to closed group.");

            if (action is ActionsGroupCloseMarker)
            {
                Close();
            }
            else
            {
                actions.Add(action);
            }
        }

        public void Close()
        {
            if (closed) throw new Exception("Can't close closed group.");

            closed = true;
            actions.Reverse();
        }

        public void Undo()
        {
            if (!closed) throw new Exception("Can't undo unclosed group.");
            foreach (IAction action in actions)
            {
                action.Undo();
            }
        }

        public IAction Redo()
        {
            if (!closed) throw new Exception("Can't redo unclosed group.");

            ActionsGroup group = new ActionsGroup();
            group.actions = new List<IAction>(actions).Select(x => x.Redo()).ToList();
            group.actions.Reverse();
            group.closed = true;

            return group;
        }
    } 
}
