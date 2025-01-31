using System;
using Microsoft.SolverFoundation.Common;

namespace Microsoft.SolverFoundation.Services
{
	internal class DerivativeHelpers
	{
		private static readonly Rational OneHalf = Rational.One / 2;

		public static int Sin(ITermModel model, int op1)
		{
			int vidNew = -1;
			model.AddOperation(TermModelOperation.Cos, out vidNew, op1);
			return vidNew;
		}

		public static int Cos(ITermModel model, int op1)
		{
			int vidNew = -1;
			int vidNew2 = -1;
			model.AddOperation(TermModelOperation.Sin, out vidNew, op1);
			model.AddOperation(TermModelOperation.Minus, out vidNew2, vidNew);
			return vidNew2;
		}

		public static int Tan(ITermModel model, int op1)
		{
			int vid = -1;
			int vidNew = -1;
			int vidNew2 = -1;
			int vidNew3 = -1;
			model.AddConstant(Rational.One, out vid);
			model.AddOperation(TermModelOperation.Tan, out vidNew, op1);
			model.AddOperation(TermModelOperation.Times, out vidNew2, vidNew, vidNew);
			model.AddOperation(TermModelOperation.Plus, out vidNew3, vid, vidNew2);
			return vidNew3;
		}

		public static int ArcSin(ITermModel model, int op1)
		{
			model.AddConstant(Rational.One, out var vid);
			model.AddOperation(TermModelOperation.Times, out var vidNew, op1, op1);
			model.AddOperation(TermModelOperation.Minus, out var vidNew2, vidNew);
			model.AddOperation(TermModelOperation.Plus, out var vidNew3, vid, vidNew2);
			model.AddOperation(TermModelOperation.Sqrt, out var vidNew4, vidNew3);
			model.AddOperation(TermModelOperation.Quotient, out var vidNew5, vid, vidNew4);
			return vidNew5;
		}

		public static int ArcCos(ITermModel model, int op1)
		{
			model.AddConstant(Rational.One, out var vid);
			model.AddOperation(TermModelOperation.Times, out var vidNew, op1, op1);
			model.AddOperation(TermModelOperation.Minus, out var vidNew2, vidNew);
			model.AddOperation(TermModelOperation.Plus, out var vidNew3, vid, vidNew2);
			model.AddOperation(TermModelOperation.Sqrt, out var vidNew4, vidNew3);
			model.AddOperation(TermModelOperation.Quotient, out var vidNew5, vid, vidNew4);
			model.AddOperation(TermModelOperation.Minus, out var vidNew6, vidNew5);
			return vidNew6;
		}

		public static int ArcTan(ITermModel model, int op1)
		{
			model.AddConstant(Rational.One, out var vid);
			model.AddOperation(TermModelOperation.Times, out var vidNew, op1, op1);
			model.AddOperation(TermModelOperation.Plus, out var vidNew2, vid, vidNew);
			model.AddOperation(TermModelOperation.Quotient, out var vidNew3, vid, vidNew2);
			return vidNew3;
		}

		public static int Sinh(ITermModel model, int op1)
		{
			model.AddOperation(TermModelOperation.Cosh, out var vidNew, op1);
			return vidNew;
		}

		public static int Cosh(ITermModel model, int op1)
		{
			model.AddOperation(TermModelOperation.Sinh, out var vidNew, op1);
			return vidNew;
		}

		public static int Tanh(ITermModel model, int op1)
		{
			model.AddConstant(Rational.One, out var vid);
			model.AddOperation(TermModelOperation.Cosh, out var vidNew, op1);
			model.AddOperation(TermModelOperation.Times, out var vidNew2, vidNew, vidNew);
			model.AddOperation(TermModelOperation.Quotient, out var vidNew3, vid, vidNew2);
			return vidNew3;
		}

		public static int Sqrt(ITermModel model, int op1)
		{
			model.AddConstant(OneHalf, out var vid);
			model.AddOperation(TermModelOperation.Sqrt, out var vidNew, op1);
			model.AddOperation(TermModelOperation.Quotient, out var vidNew2, vid, vidNew);
			return vidNew2;
		}

		public static int Log(ITermModel model, int op1)
		{
			model.AddConstant(Rational.One, out var vid);
			model.AddOperation(TermModelOperation.Quotient, out var vidNew, vid, op1);
			return vidNew;
		}

		public static int Log10(ITermModel model, int op1)
		{
			model.AddConstant(1.0 / Math.Log(10.0), out var vid);
			model.AddOperation(TermModelOperation.Quotient, out var vidNew, vid, op1);
			return vidNew;
		}
	}
}
