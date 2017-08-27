﻿using System;
using System.Linq;

namespace Fougerite.Patcher
{
    internal class Program
    {
        public const string Version = "1.7.7";

        private static void Main(string[] args)
        {
            bool firstPass = args.Contains("-1");
            bool secondPass = args.Contains("-2");

            Logger.Clear();

            if (!firstPass && !secondPass) 
            {
                Logger.Log("Fougerite Patcher V" + Version);
                Logger.Log("No command specified.");
                Logger.Log("Launch patcher with args: \"-1\" (fields update) or\\and \"-2\" (methods update).");
                Logger.Log("Or enter \"0\" to patch with both flags");
                if (Console.ReadLine() == "0")
                {
                    firstPass = true;
                    secondPass = true;
                }
            }

            ILPatcher patcher = new ILPatcher();

            bool result = true;
            if (firstPass) 
            {
                result = result && patcher.FirstPass();
            }

            if (secondPass) 
            {
                result = result && patcher.SecondPass();
            }

            if (result) {
                Logger.Log("The patch was applied successfully!");
            }
            Console.ReadLine();
        }
    }
}
