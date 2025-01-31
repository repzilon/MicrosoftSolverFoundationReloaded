using System;
using System.Collections.Generic;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>Contains a number of static utilities for advanced users
	///          of the Local Search framework
	/// </summary>
	internal static class LocalSearch
	{
		/// <summary>Delegate type for a restart strategy, called at every
		///          iteration of a local search loop
		/// </summary>
		/// <param name="solver">The local search solver that is calling this
		///          delegate to ask for advice
		/// </param>
		/// <remarks>Unless it is stateless, the delegate should not be associated
		///          to several ILocalSearchSolver objects
		/// </remarks>
		/// <returns>True when a restart is advised. This will in general mean that
		///          we have detected that the local search is stuck in a local
		///          minimum and should try to escape from it
		/// </returns>
		public delegate bool RestartStrategy(ILocalSearchProcess solver);

		/// <summary>Delegate type for an initialization strategy, called every 
		///          time a local search solver (re)-starts to a new point
		/// </summary>
		/// <param name="solver">The local search solver that is calling this
		///          delegate to ask for advice
		/// </param>
		/// <remarks>Unless it is stateless, the delegate should not be associated
		///          to several ILocalSearchSolver objects
		/// </remarks>
		/// <returns>A Dictionary describing the next new starting point. 
		///          Defines a new value for some (not necessarily all) of the
		///          variables of the problem
		/// </returns>
		public delegate Dictionary<CspTerm, int> InitializationStrategy(ILocalSearchProcess solver);

		/// <summary>Delegate type for a move strategy, called at every iteration
		///          of a local search loop to indicate which change should be done
		/// </summary>
		/// <param name="solver">The local search solver that is calling this
		///          delegate to ask for advice
		/// </param>
		/// <remarks>Unless it is stateless, the delegate should not be associated
		///          to several ILocalSearchSolver objects
		/// </remarks>
		/// <returns>A Move indicating which change the local search algorithm 
		///          should perform
		/// </returns>
		public delegate Move MoveStrategy(ILocalSearchProcess solver);

		/// <summary>Delegate type for a move strategy, called every time a move
		///          is performed, to decide whether the new configuration should
		///          be accepted
		/// </summary>
		/// <param name="solver">The local search solver that is calling this
		///          delegate to ask for advice
		/// </param>
		/// <param name="move">move that has just been performed</param>
		/// <param name="moveCost">estimate of the cost of undoing the move</param>
		/// <param name="qualityChange">A numerical indicator of whether how the
		///          penalty of the configuration has changed.
		///          The lower the value, the better the move:
		///          0 if it was unchanged;
		///          a strictly negative value if the penalty decreased;
		///          a strictly positive valie of the penalty increased.
		/// </param>
		/// <remarks>Unless it is stateless, the delegate should not be associated
		///          to several ILocalSearchSolver objects
		/// </remarks>
		/// <returns>True iff the move should be accepted
		///          otherwise it will be undone
		/// </returns>
		public delegate bool AcceptanceStrategy(ILocalSearchProcess solver, Move move, int qualityChange, int moveCost);

		/// <summary>Delegate that can be subscribed to a local search algorithm
		///          and that is then called every time the algorithm attempts
		///          a move
		/// </summary>
		/// <remarks>Even the failed attempts of move cause a call-back</remarks>
		/// <param name="move">change that was just attempted</param>
		/// <param name="improvedQuality">true if the move improved the quality</param>
		/// <param name="accepted">true if the move was effectively accepted</param>
		public delegate void MoveListener(Move move, bool improvedQuality, bool accepted);

		/// <summary>A move, indicating a state that can be performed by a
		///          local search solver
		/// </summary>
		/// <remarks>The only legal way to construct Moves is through the
		///          factory methods of the LocalSearch class
		/// </remarks>
		public struct Move
		{
			/// <summary>The type of move</summary>
			public readonly MoveType Type;

			/// <summary>Optional value of the move</summary>
			public readonly int Value;

			/// <summary>Primary variable affected by the move;
			///          Null only if the move does not affect anything
			/// </summary>
			internal readonly CspVariable Var1;

			/// <summary>Optional variable affected by the move;
			///          Non-null only if the move is a variable swap
			/// </summary>
			internal readonly CspVariable Var2;

			/// <summary>Primary variable affected by the move;
			///          Precondition: the move should affect at least one variable
			/// </summary>
			public CspTerm FirstVariable => Var1;

			/// <summary>Secondary variable affected by the move;
			///          Precondition: the move should affect 2 variables
			/// </summary>
			public CspTerm SecondVariable => Var2;

			internal Move(MoveType type, CspVariable x1, CspVariable x2, int val)
			{
				Type = type;
				Var1 = x1;
				Var2 = x2;
				Value = val;
			}
		}

		/// <summary>Possible types of moves </summary>
		internal enum MoveType
		{
			Stop = 1,
			Flip,
			Swap
		}

		internal const int _scaleDownLimit = 1000;

		/// <summary>An arbitrary, but fixed, number to initialize random sequences
		/// </summary>
		private static int _defaultRandomSeedInitializer = 1234567890;

		private static readonly int _scaleDownShift = 1000 - (int)Math.Sqrt(1001.0);

		internal static Func<int, int> _scaleDownLogarithmically = ScaleDownLogarithmically;

		internal static Func<int, int> _scaleDownUsingRoot = ScaleDownUsingRoot;

		/// <summary>Returns a simple restart strategy in which we never restart
		/// </summary>
		public static RestartStrategy RestartNever()
		{
			return (ILocalSearchProcess solver) => false;
		}

		/// <summary>Returns a basic restart strategy in which try to detect when we 
		///          are stuck for too long in a local minima because the percentage
		///          of recent moves that improved the score is below a threshold
		/// </summary>
		/// <param name="threshold">A percentage that tunes how agressively we restart:
		///         restart will happen when the percentage of improved moves becomes
		///         lower than this threshold (higher values therefore mean more agressive)
		/// </param>
		public static RestartStrategy RestartWhenThresholdHit(double threshold)
		{
			LS_ThresholdEscapeStrategy @object = new LS_ThresholdEscapeStrategy(threshold);
			return @object.Restart;
		}

		/// <summary>Returns a basic restart strategy in which try to detect when we 
		///          are stuck for too long in a local minimum because the number
		///          of moves that did not improve the quality is larger than a limit
		/// </summary>
		public static RestartStrategy RestartWhenStuckForTooLong()
		{
			LS_SimpleMinimaEscapeStrategy @object = new LS_SimpleMinimaEscapeStrategy();
			return @object.Restart;
		}

		/// <summary>Returns a simple initialization strategy in which all or some
		///          of the variables are re-set to a value picked uniformly at
		///          random from their domain
		/// </summary>
		/// <param name="randomSeed">A seed for the Random generator used in this 
		///          strategy. Set to fixed value by default
		/// </param>
		/// <param name="percentage">A value between 0 and 1 saying which percentage
		///          of the variables are perturbated on a typical restart
		/// </param>
		public static InitializationStrategy InitializeRandomly(int randomSeed, double percentage)
		{
			LS_RandomInitializationStrategy @object = new LS_RandomInitializationStrategy(randomSeed, percentage);
			return @object.NextConfiguration;
		}

		/// <summary>Returns a classic acceptance strategy in which a configuration 
		///          is accepted iff it does not degrade the quality
		/// </summary>
		public static AcceptanceStrategy AcceptIfImproved()
		{
			return (ILocalSearchProcess solver, Move move, int qualityChange, int moveCost) => qualityChange <= 0;
		}

		/// <summary>Returns an acceptance strategy in which moves are always
		///          accepted
		/// </summary>
		public static AcceptanceStrategy AcceptAlways()
		{
			return (ILocalSearchProcess solver, Move move, int qualityChange, int moveCost) => true;
		}

		/// <summary>Returns an acceptance strategy in which a configuration is always 
		///          accepted if it improves the quality, and accepted with some
		///          probability P when the quality is degraded
		/// </summary>
		/// <param name="probability">the probability of acceptance</param>
		/// <param name="randomSeed">A seed for the Random generator used in this 
		///          strategy. Set to fixed value by default
		/// </param>
		public static AcceptanceStrategy AcceptProbabilistically(double probability, int randomSeed)
		{
			probability = Math.Min(probability, 0.0);
			probability = Math.Max(probability, 1.0);
			Random prng = new Random(randomSeed);
			return (ILocalSearchProcess solver, Move change, int qualityChange, int flipCost) => qualityChange <= 0 || prng.NextDouble() < probability;
		}

		public static AcceptanceStrategy ProbabilisticAcceptanceStrategy(double probability)
		{
			return AcceptProbabilistically(probability, _defaultRandomSeedInitializer);
		}

		/// <summary>Returns a probabilistic acceptance strategy in which the probability
		///          of acceptance evolves with time according to a temperature factor
		/// </summary>
		/// <param name="randomSeed">A seed for the Random generator used in this 
		///          strategy. Set to fixed value by default
		/// </param>
		public static AcceptanceStrategy AcceptWithSimulatedAnnealing(int randomSeed)
		{
			LS_SimulatedAnnealingAcceptanceStrategy @object = new LS_SimulatedAnnealingAcceptanceStrategy(randomSeed);
			return @object.Accept;
		}

		public static AcceptanceStrategy SimulatedAnnealingAcceptanceStrategy()
		{
			return AcceptWithSimulatedAnnealing(_defaultRandomSeedInitializer);
		}

		/// <summary>Returns a move strategy that flips variables at random
		/// </summary>
		/// <remarks>The selection of the value taken for the variable is not
		///          complitely random but biased towards a good quality, so that
		///          the search is not completely blind
		/// </remarks>
		/// <param name="randomSeed">A seed for the Random generator used in this 
		///          strategy. Set to fixed value by default
		/// </param>
		public static MoveStrategy MoveRandomly(int randomSeed)
		{
			Random prng = new Random(randomSeed);
			return delegate(ILocalSearchProcess arg)
			{
				LocalSearchSolver localSearchSolver = arg as LocalSearchSolver;
				KeyValuePair<CspTerm, int> keyValuePair = localSearchSolver.RandomFlip(prng);
				return CreateVariableFlip(keyValuePair.Key, keyValuePair.Value);
			};
		}

		public static MoveStrategy MoveRandomly()
		{
			return MoveRandomly(_defaultRandomSeedInitializer);
		}

		/// <summary>Returns a move strategy where the moves are random flips  
		///          that are computed in a constraint-driven way
		/// </summary>
		/// <param name="randomSeed">A seed for the Random generator used in this 
		///          strategy. Set to fixed value by default
		/// </param>
		public static MoveStrategy MoveByConstraintGuidedFlips(int randomSeed)
		{
			LS_ConstraintGuidedMoveStrategy @object = new LS_ConstraintGuidedMoveStrategy(randomSeed);
			return @object.NextMove;
		}

		public static MoveStrategy MoveByConstraintGuidedFlips()
		{
			return MoveByConstraintGuidedFlips(_defaultRandomSeedInitializer);
		}

		/// <summary>Returns a move strategy where the moves are random flips  
		///          that are computed in a gradient-driven way
		/// </summary>
		/// <param name="randomSeed">A seed for the Random generator used in this 
		///          strategy. Set to fixed value by default
		/// </param>
		public static MoveStrategy MoveByGradientGuidedFlips(int randomSeed)
		{
			LS_GradientGuidedMoveStrategy @object = new LS_GradientGuidedMoveStrategy(randomSeed);
			return @object.NextMove;
		}

		public static MoveStrategy MoveByGradientGuidedFlips()
		{
			return MoveByGradientGuidedFlips(_defaultRandomSeedInitializer);
		}

		/// <summary>Returns a move strategy in which the moves are as follows:
		///          If the constraints are violated then a constraint-guided move
		///          is selected aiming at repairing them;
		///          If the constraints are satisfied then a sample of moves is
		///          considered and the most promising is selected
		/// </summary>
		/// <param name="randomSeed">A seed for the Random generator used in this 
		///          strategy. Set to fixed value by default
		/// </param>
		public static MoveStrategy MoveByGreedyImprovements(int randomSeed)
		{
			LS_GreedyImprovementStrategy @object = new LS_GreedyImprovementStrategy(randomSeed);
			MoveStrategy guided = MoveByConstraintGuidedFlips(randomSeed);
			MoveStrategy greedy = @object.NextMove;
			return (ILocalSearchProcess solver) => (solver.CurrentViolation > 0) ? guided(solver) : greedy(solver);
		}

		/// <summary>A move strategy that uses a Tabu list, i.e. has a short-term
		///          memory of forbidden moves. At every step the strategy tries
		///          to find a good move that is not marked Tabu
		/// </summary>
		/// <param name="randomSeed">A seed for the Random generator used in this 
		///          strategy. Set to fixed value by default
		/// </param>
		/// <param name="tabuListLength">Number of iterations for which a variable
		///          flip remains Tabu. Set by default according to a slowly 
		///          increasing function of the number of variables
		/// </param>
		public static MoveStrategy MoveUsingTabu(int randomSeed, int tabuListLength)
		{
			LS_TabuMoveStrategy @object = new LS_TabuMoveStrategy(randomSeed, tabuListLength);
			return @object.NextMove;
		}

		public static MoveStrategy MoveUsingTabu(int randomSeed)
		{
			LS_TabuMoveStrategy @object = new LS_TabuMoveStrategy(randomSeed);
			return @object.NextMove;
		}

		public static MoveStrategy MoveUsingTabu()
		{
			LS_TabuMoveStrategy @object = new LS_TabuMoveStrategy(_defaultRandomSeedInitializer);
			return @object.NextMove;
		}

		/// <summary>Creates a Move whose effect is to flip a certain
		///          variable to a certain value
		/// </summary>
		/// <param name="target">The term that is being flipped. 
		///          This term must be a used-defined variable
		/// </param>
		/// <param name="value">The new value for the term. 
		///          This value must belong to the initial domain of the variable
		/// </param>
		public static Move CreateVariableFlip(CspTerm target, int value)
		{
			CspVariable x = target as CspVariable;
			return new Move(MoveType.Flip, x, null, value);
		}

		/// <summary>Creates a Move whose effect is to swap the
		///          values of two variables
		/// </summary>
		/// <param name="var1">A term, which must be a user-defined variable</param>
		/// <param name="var2">A term, which must be a user-defined variable</param>
		public static Move CreateVariableSwap(CspTerm var1, CspTerm var2)
		{
			CspVariable x = var1 as CspVariable;
			CspVariable x2 = var2 as CspVariable;
			return new Move(MoveType.Swap, x, x2, int.MinValue);
		}

		/// <summary>Returns a null move, meaning that no suggestion of improvement
		///          can be found in the current configuration.
		///          This effectively tells the local search algorithm to restart
		///          if no other strategy applies next
		/// </summary>
		public static Move CreateStop()
		{
			return new Move(MoveType.Stop, null, null, int.MinValue);
		}

		/// <summary>A function that increases very slowly
		/// </summary>
		internal static int ScaleDownLogarithmically(int val)
		{
			int num = 0;
			while (val != 0)
			{
				num++;
				val >>= 1;
			}
			return num;
		}

		/// <summary>For small values the function returns the value unchanged,
		///          when the number gets higher the function grows fairly slowly
		/// </summary>
		/// <remarks>Use when an iteration needs to take into account a range
		///          but should not grow unreasonably large if the range is large.
		/// </remarks>
		internal static int ScaleDownUsingRoot(int x)
		{
			if (x <= 1000)
			{
				return x;
			}
			int num = (int)Math.Sqrt(x);
			return num + _scaleDownShift;
		}
	}
}
