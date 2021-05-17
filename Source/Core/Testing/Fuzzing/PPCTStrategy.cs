﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Testing.Fuzzing
{
    /// <summary>
    /// A probabilistic fuzzing strategy.
    /// </summary>
    internal class PPCTStrategy : FuzzingStrategy
    {
        /// <summary>
        /// Random value generator.
        /// </summary>
        protected IRandomValueGenerator RandomValueGenerator;

        /// <summary>
        /// The maximum number of steps to explore.
        /// </summary>
        protected readonly int MaxSteps;

        /// <summary>
        /// The maximum number of steps after which we should reshuffle the probabilities.
        /// </summary>
        protected readonly int PriorityChangePoints;

        private readonly List<int> LowPrioritySet = new List<int>();

        private readonly List<int> HighPrioritySet = new List<int>();

        private double lowPriortityProbability;

        /// <summary>
        /// The number of exploration steps.
        /// </summary>
        protected int StepCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="PPCTStrategy"/> class.
        /// </summary>
        internal PPCTStrategy(IRandomValueGenerator random, int maxDelays = 500, int priorityChangePoints = 2)
        {
            this.RandomValueGenerator = random;
            this.MaxSteps = maxDelays;
            this.PriorityChangePoints = priorityChangePoints;
            this.lowPriortityProbability = 0;
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration)
        {
            this.StepCount = 0;
            this.LowPrioritySet.Clear();
            this.HighPrioritySet.Clear();

            // Change the probability of a task to be assigned to the LowPrioritySet after every iteration.
            this.lowPriortityProbability = (this.lowPriortityProbability >= 0.8) ? 0 : this.lowPriortityProbability + 0.1;

            return true;
        }

        /// <inheritdoc/>
        internal override bool GetNextDelay(int maxValue, out int next)
        {
            int? currentTaskId = Task.CurrentId;
            if (currentTaskId == null)
            {
                next = 0;
                return true;
            }

            this.StepCount++;

            // Reshuffle the probabilities after every (this.MaxSteps / this.PriorityChangePoints) steps.
            if (this.StepCount % (this.MaxSteps / 100) == 0)
            {
                this.LowPrioritySet.Clear();
                this.HighPrioritySet.Clear();
            }

            // If this task is not assigned to any Low/High priority group.
            if (!this.LowPrioritySet.Contains((int)currentTaskId) && !this.HighPrioritySet.Contains((int)currentTaskId))
            {
                // Randomly assign a Task to long/short delay group.
                if (this.RandomValueGenerator.NextDouble() < this.lowPriortityProbability)
                {
                    this.LowPrioritySet.Add((int)currentTaskId);
                }
                else
                {
                    this.HighPrioritySet.Add((int)currentTaskId);
                }
            }

            // If this Task lies in the HighPrioritySet, we will return a delay of 1ms else 10ms.
            if (this.HighPrioritySet.Contains((int)currentTaskId))
            {
                next = 0;
            }
            else
            {
                if (this.lowPriortityProbability > 0.4)
                {
                    next = this.RandomValueGenerator.Next(10) * 5;
                }
                else
                {
                    next = (this.RandomValueGenerator.Next(10) * 5) + 50;
                }
            }

            return true;
        }

        /// <inheritdoc/>
        internal override int GetStepCount() => this.StepCount;

        /// <inheritdoc/>
        internal override bool IsMaxStepsReached()
        {
            return false;
        }

        /// <inheritdoc/>
        internal override bool IsFair() => true;

        /// <inheritdoc/>
        internal override string GetDescription() => $"PPCT fuzzing";
    }
}
