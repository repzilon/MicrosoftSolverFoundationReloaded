using System;
using System.Globalization;
using System.Reflection;
using System.Threading;
using Microsoft.SolverFoundation.Common;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Rewrite
{
	internal sealed class ReflectionBinder : Binder
	{
		private static ReflectionBinder _rb;

		public static ReflectionBinder Instance
		{
			get
			{
				if (_rb == null)
				{
					Interlocked.CompareExchange(ref _rb, new ReflectionBinder(), null);
				}
				return _rb;
			}
		}

		private ReflectionBinder()
		{
		}

		public override FieldInfo BindToField(BindingFlags bindingAttr, FieldInfo[] match, object value, CultureInfo culture)
		{
			throw new NotImplementedException(Resources.TheMethodOrOperationIsNotImplemented);
		}

		public override MethodBase BindToMethod(BindingFlags flags, MethodBase[] rgmeth, ref object[] rgarg, ParameterModifier[] rgmod, CultureInfo culture, string[] rgname, out object objState)
		{
			objState = null;
			if (rgmeth == null)
			{
				return null;
			}
			if (rgname != null)
			{
				return null;
			}
			int num = -1;
			int num2 = -1;
			for (int i = 0; i < rgmeth.Length; i++)
			{
				int num3 = 0;
				ParameterInfo[] parameters = rgmeth[i].GetParameters();
				if (rgarg.Length != parameters.Length)
				{
					continue;
				}
				int num4 = 0;
				while (true)
				{
					if (num4 == rgarg.Length)
					{
						if (num3 == rgarg.Length)
						{
							return rgmeth[i];
						}
						if (num2 < num3)
						{
							num2 = num3;
							num = i;
						}
						break;
					}
					if (!CanConvert(null, rgarg[num4], parameters[num4].ParameterType, out var fExact, out var _))
					{
						break;
					}
					if (fExact)
					{
						num3++;
					}
					num4++;
				}
			}
			if (num >= 0)
			{
				return rgmeth[num];
			}
			return null;
		}

		public static bool CanConvert(Type typeArg, object arg, Type typeParam, out bool fExact, out object val)
		{
			fExact = false;
			if (typeArg == null)
			{
				if (arg == null)
				{
					val = null;
					return false;
				}
				typeArg = arg.GetType();
			}
			if (typeArg == typeParam || typeParam.IsAssignableFrom(typeArg))
			{
				fExact = true;
				val = arg;
				return true;
			}
			if (typeParam.IsValueType && typeArg.IsValueType)
			{
				if (typeArg == typeof(BigInteger))
				{
					if (arg == null)
					{
						val = null;
						if (!(typeParam == typeof(Rational)) && !(typeParam == typeof(double)))
						{
							return typeParam == typeof(float);
						}
						return true;
					}
					BigInteger bigInteger = (BigInteger)arg;
					if (typeParam == typeof(Rational))
					{
						val = (Rational)bigInteger;
						return true;
					}
					if (typeParam == typeof(double))
					{
						val = (double)bigInteger;
						return true;
					}
					if (typeParam == typeof(float))
					{
						val = (float)(double)bigInteger;
						return true;
					}
					return CanConvertInteger(bigInteger, typeParam, out val);
				}
				if (typeArg == typeof(Rational))
				{
					if (arg == null)
					{
						val = null;
						if (!(typeParam == typeof(double)))
						{
							return typeParam == typeof(float);
						}
						return true;
					}
					Rational rational = (Rational)arg;
					if (typeParam == typeof(double))
					{
						val = (double)rational;
						return true;
					}
					if (typeParam == typeof(float))
					{
						val = (float)(double)rational;
						return true;
					}
					if (rational.IsInteger(out var bn))
					{
						return CanConvertInteger(bn, typeParam, out val);
					}
				}
				else if (typeArg == typeof(double))
				{
					if (arg == null)
					{
						val = null;
						if (!(typeParam == typeof(Rational)))
						{
							return typeParam == typeof(float);
						}
						return true;
					}
					double num = (double)arg;
					if (typeParam == typeof(float))
					{
						val = (float)num;
						return true;
					}
					if (typeParam == typeof(Rational))
					{
						val = (Rational)num;
						return true;
					}
					BigInteger bigInteger2 = (BigInteger)num;
					if (bigInteger2 == num)
					{
						return CanConvertInteger(bigInteger2, typeParam, out val);
					}
				}
			}
			val = null;
			return false;
		}

		private static bool CanConvertInteger(BigInteger bn, Type typeParam, out object val)
		{
			if (typeParam == typeof(int))
			{
				int num = (int)bn;
				val = num;
				return num == bn;
			}
			if (typeParam == typeof(uint))
			{
				uint num2 = (uint)bn;
				val = num2;
				return num2 == bn;
			}
			if (typeParam == typeof(long))
			{
				long num3 = (long)bn;
				val = num3;
				return num3 == bn;
			}
			if (typeParam == typeof(ulong))
			{
				ulong num4 = (ulong)bn;
				val = num4;
				return num4 == bn;
			}
			if (typeParam == typeof(BigInteger))
			{
				val = bn;
				return true;
			}
			val = null;
			return false;
		}

		public override object ChangeType(object val, Type type, CultureInfo culture)
		{
			if (CanConvert(null, val, type, out var _, out val))
			{
				return val;
			}
			return null;
		}

		public override MethodBase SelectMethod(BindingFlags bindingAttr, MethodBase[] match, Type[] types, ParameterModifier[] modifiers)
		{
			throw new NotImplementedException(Resources.TheMethodOrOperationIsNotImplemented);
		}

		public override PropertyInfo SelectProperty(BindingFlags bindingAttr, PropertyInfo[] match, Type returnType, Type[] indexes, ParameterModifier[] modifiers)
		{
			throw new NotImplementedException(Resources.TheMethodOrOperationIsNotImplemented);
		}

		public override void ReorderArgumentArray(ref object[] args, object state)
		{
			throw new NotImplementedException(Resources.TheMethodOrOperationIsNotImplemented);
		}
	}
}
