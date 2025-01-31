using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Solvers
{
	/// <summary>
	/// A composite data type that is consist of finite set of Term members each of which has its own domain.
	/// </summary>
	public class CspComposite
	{
		internal struct Tuple
		{
			internal object Key;

			internal int Arity;

			internal CspDomain Domain;

			internal CspTerm[] Vars;

			internal Tuple(object key, int arity, CspDomain domain, CspTerm[] vars)
			{
				Key = key;
				Arity = arity;
				Domain = domain;
				Vars = vars;
			}
		}

		private bool _fVarCreated;

		private object _key;

		private ConstraintSystem _baseConstraintSystem;

		private ConstraintSystem _compositeConstraints;

		private Dictionary<object, Tuple> _mapKeyTuple;

		private List<Tuple> _fields;

		/// <summary>
		/// Return true if and only if the model in the solver is empty
		/// </summary>
		public bool IsEmpty => ConstraintContainer.AllTerms.Count == 0;

		/// <summary> The Boolean Term {true} which is immutable and can be used anywhere you need a true constant.
		/// </summary>
		public CspTerm True => ConstraintContainer.True;

		/// <summary> The Boolean Term {false} which is immutable and can be used anywhere you need a false constant.
		/// </summary>
		public CspTerm False => ConstraintContainer.False;

		/// <summary>
		/// Get the key of this composite
		/// </summary>
		public object Key => _key;

		internal ConstraintSystem BaseSolver => _baseConstraintSystem;

		internal ConstraintSystem ConstraintContainer => _compositeConstraints;

		internal List<Tuple> AllFields => _fields;

		internal CspComposite(ConstraintSystem solver, object key)
		{
			if (key != null)
			{
				_key = key;
			}
			else
			{
				_key = this;
			}
			solver.AddComposite(this);
			_baseConstraintSystem = solver;
			_compositeConstraints = new ConstraintSystem();
			_compositeConstraints.Mode = _baseConstraintSystem.Mode;
			_fVarCreated = false;
			_mapKeyTuple = new Dictionary<object, Tuple>();
			_fields = new List<Tuple>();
		}

		/// <summary> Get a Term for the immutable real value k with the default precision
		/// </summary>
		public CspTerm Constant(double k)
		{
			return Register(ConstraintContainer.Constant(k));
		}

		/// <summary> Get a Term for the immutable value k
		/// </summary>
		public CspTerm Constant(int k)
		{
			return Register(ConstraintContainer.Constant(k));
		}

		/// <summary> Get a Term for the immutable real value k
		/// <param name="precision">Only allows 1, 10, 100, 1000, and 1000</param>
		/// <param name="k">Value</param>
		/// </summary>
		public CspTerm Constant(int precision, double k)
		{
			return Register(ConstraintContainer.Constant(precision, k));
		}

		/// <summary>
		/// Get a Term for the immutable symbol value k
		/// </summary>
		/// <param name="stringValueSet">The domain that the constant belongs to</param>
		/// <param name="symbol">the value of the constant</param>
		/// <returns></returns>
		public CspTerm Constant(CspDomain stringValueSet, string symbol)
		{
			return Register(ConstraintContainer.Constant(stringValueSet, symbol));
		}

		/// <summary> This function is the absolute value of its input.
		/// </summary>
		public CspTerm Abs(CspTerm input)
		{
			return Register(ConstraintContainer.Abs(input));
		}

		/// <summary> This function is a Boolean AND of its Boolean inputs.
		/// </summary>
		public CspTerm And(params CspTerm[] inputs)
		{
			return Register(ConstraintContainer.And(inputs));
		}

		/// <summary> This function is true iff at most m of its Boolean inputs are true.
		/// </summary>
		public CspTerm AtMostMofN(int m, params CspTerm[] inputs)
		{
			return Register(ConstraintContainer.AtMostMofN(m, inputs));
		}

		/// <summary> This function is true iff all of its inputs are equal to the constant.
		/// </summary>
		public CspTerm Equal(int constant, params CspTerm[] inputs)
		{
			return Register(ConstraintContainer.Equal(constant, inputs));
		}

		/// <summary> This function is true iff all of its inputs are equal.
		/// </summary>
		public CspTerm Equal(params CspTerm[] inputs)
		{
			return Register(ConstraintContainer.Equal(inputs));
		}

		/// <summary> This function is true iff input is a value in the domain.
		/// </summary>
		public CspTerm IsElementOf(CspTerm input, CspDomain domain)
		{
			return Register(ConstraintContainer.IsElementOf(input, domain));
		}

		/// <summary> This function is true iff exactly m of its Boolean inputs are true.
		/// </summary>
		public CspTerm ExactlyMofN(int m, params CspTerm[] inputs)
		{
			return Register(ConstraintContainer.ExactlyMofN(m, inputs));
		}

		/// <summary> This function is the sum of the conditional inputs.
		/// </summary>
		/// <remarks> The input vectors must be of equal length </remarks>
		public CspTerm FilteredSum(CspTerm[] conditions, CspTerm[] inputs)
		{
			return Register(ConstraintContainer.FilteredSum(conditions, inputs));
		}

		/// <summary> This function is true iff each of its inputs is greater than the following input.
		/// </summary>
		public CspTerm Greater(int constant, params CspTerm[] inputs)
		{
			return Register(ConstraintContainer.Greater(constant, inputs));
		}

		/// <summary> This function is true iff each of its inputs is greater than the following input.
		/// </summary>
		public CspTerm Greater(params CspTerm[] inputs)
		{
			return Register(ConstraintContainer.Greater(inputs));
		}

		/// <summary> This function is true iff each of its inputs is greater than or equal to the following input.
		/// </summary>
		public CspTerm GreaterEqual(int constant, params CspTerm[] inputs)
		{
			return Register(ConstraintContainer.GreaterEqual(constant, inputs));
		}

		/// <summary> This function is true iff each of its inputs is greater than or equal to the following input.
		/// </summary>
		public CspTerm GreaterEqual(params CspTerm[] inputs)
		{
			return Register(ConstraintContainer.GreaterEqual(inputs));
		}

		/// <summary> This function is a Boolean implication of the form antecedent -&gt; consequent
		/// </summary>
		public CspTerm Implies(CspTerm antecedent, CspTerm consequent)
		{
			return Register(ConstraintContainer.Implies(antecedent, consequent));
		}

		/// <summary> This function is the value of the input selected by the keys,
		///           which map into the data with the first key being most major.
		///           This allows generalization to multiple keys, though we choose
		///           to represent the terms as a single vector instead of an
		///           arbitrary-dimension array (which in C# is awkward to build or use).
		/// </summary>
		public CspTerm Index(CspTerm[] inputs, params CspTerm[] keys)
		{
			return Register(ConstraintContainer.Index(inputs, keys));
		}

		/// <summary> This function is the value of the [index] input.
		/// </summary>
		public CspTerm Index(CspTerm[] inputs, CspTerm index)
		{
			return Register(ConstraintContainer.Index(inputs, index));
		}

		/// <summary> This function is the value of the [row][column] input
		/// </summary>
		public CspTerm Index(CspTerm[][] inputs, CspTerm row, int column)
		{
			return Register(ConstraintContainer.Index(inputs, row, column));
		}

		/// <summary> This function is the value of the [row][column] input
		/// </summary>
		public CspTerm Index(CspTerm[][] inputs, int row, CspTerm column)
		{
			return Register(ConstraintContainer.Index(inputs, row, column));
		}

		/// <summary> This function is the value of the [row][column] input
		/// </summary>
		public CspTerm Index(CspTerm[][] inputs, CspTerm row, CspTerm column)
		{
			return Register(ConstraintContainer.Index(inputs, row, column));
		}

		/// <summary> This function is true iff each of its inputs is less than the following input.
		/// </summary>
		public CspTerm Less(int constant, params CspTerm[] inputs)
		{
			return Register(ConstraintContainer.Less(constant, inputs));
		}

		/// <summary> This function is true iff each of its inputs is less than the following input.
		/// </summary>
		public CspTerm Less(params CspTerm[] inputs)
		{
			return Register(ConstraintContainer.Less(inputs));
		}

		/// <summary> This function is true iff each of its inputs is less than or equal to the following input.
		/// </summary>
		public CspTerm LessEqual(int constant, params CspTerm[] inputs)
		{
			return Register(ConstraintContainer.LessEqual(constant, inputs));
		}

		/// <summary> This function is true iff each of its inputs is less than or equal to the following input.
		/// </summary>
		public CspTerm LessEqual(params CspTerm[] inputs)
		{
			return Register(ConstraintContainer.LessEqual(inputs));
		}

		/// <summary> This function is the negation of its input.
		/// </summary>
		public CspTerm Neg(CspTerm input)
		{
			return Register(ConstraintContainer.Neg(input));
		}

		/// <summary> This function is a Boolean inverse of its Boolean input.
		/// </summary>
		public CspTerm Not(CspTerm input)
		{
			return Register(ConstraintContainer.Not(input));
		}

		/// <summary> This function is a Boolean OR of its Boolean inputs.
		/// </summary>
		public CspTerm Or(params CspTerm[] inputs)
		{
			return Register(ConstraintContainer.Or(inputs));
		}

		/// <summary> This function is its input raised to the power.
		/// </summary>
		public CspTerm Power(CspTerm x, int power)
		{
			return Register(ConstraintContainer.Power(x, power));
		}

		/// <summary> This function is the product of its inputs.
		/// </summary>
		public CspTerm Product(params CspTerm[] inputs)
		{
			return Register(ConstraintContainer.Product(inputs));
		}

		/// <summary> This function is the sum of its inputs.
		/// </summary>
		public CspTerm Sum(params CspTerm[] inputs)
		{
			return Register(ConstraintContainer.Sum(inputs));
		}

		/// <summary> This function is the sum of the pairwise-product of its inputs.
		/// </summary>
		/// <remarks> The input vectors must be of equal length </remarks>
		public CspTerm SumProduct(CspTerm[] inputs1, CspTerm[] inputs2)
		{
			return Register(ConstraintContainer.SumProduct(inputs1, inputs2));
		}

		/// <summary> This function is true iff every pairing of its inputs is unequal.
		/// </summary>
		public CspTerm Unequal(int constant, params CspTerm[] inputs)
		{
			return Register(ConstraintContainer.Unequal(constant, inputs));
		}

		/// <summary> This function is true iff every pairing of its inputs is unequal.
		/// </summary>
		public CspTerm Unequal(params CspTerm[] inputs)
		{
			return Register(ConstraintContainer.Unequal(inputs));
		}

		/// <summary>
		/// This function creates a relation constraint using the column variables and the
		/// inputs. colVars define the variables for all columns. inputs are rows of valid
		/// value combinations of column variables.
		/// </summary>
		public CspTerm TableTerm(CspTerm[] colVars, IEnumerable<IEnumerable<CspTerm>> inputs)
		{
			return Register(ConstraintContainer.TableTerm(colVars, inputs));
		}

		/// <summary>
		/// This function creates a relation constraint using the column variables and the
		/// inputs. colVars define the variables for all columns. inputs are rows of valid
		/// Domain combinations of column variables.
		/// </summary>
		public CspTerm TableDomain(CspTerm[] colVars, IEnumerable<IEnumerable<CspDomain>> inputs)
		{
			return Register(ConstraintContainer.TableDomain(colVars, inputs));
		}

		/// <summary>
		/// This function creates a relation constraint using the column variables and the
		/// inputs. colVars define the variables for all columns. inputs are rows of valid
		/// value combinations of column variables.
		/// </summary>
		public CspTerm TableDecimal(CspTerm[] colVars, params double[][] inputs)
		{
			return Register(ConstraintContainer.TableDecimal(colVars, inputs));
		}

		/// <summary>
		/// This function creates a relation constraint using the column variables and the
		/// inputs. colVars define the variables for all columns. inputs are rows of valid
		/// value combinations of column variables.
		/// </summary>
		public CspTerm TableInteger(CspTerm[] colVars, params int[][] inputs)
		{
			return Register(ConstraintContainer.TableInteger(colVars, inputs));
		}

		/// <summary>
		/// This function creates a relation constraint using the column variables and the
		/// inputs. colVars define the variables for all columns. inputs are rows of valid
		/// value combinations of column variables.
		/// </summary>
		public CspTerm TableSymbol(CspTerm[] colVars, params string[][] inputs)
		{
			return Register(ConstraintContainer.TableSymbol(colVars, inputs));
		}

		/// <summary>
		/// This function creates a relation constraint using the column variables and the
		/// inputs. colVars define the variables for all columns. inputs are rows of valid
		/// Domain combinations of column variables.
		/// </summary>
		public CspTerm TableTerm(CspTerm[] colVars, params CspTerm[][] inputs)
		{
			return Register(ConstraintContainer.TableTerm(colVars, inputs));
		}

		/// <summary>
		/// This function creates a relation constraint using the column variables and the
		/// inputs. colVars define the variables for all columns. inputs are rows of valid
		/// Domain combinations of column variables.
		/// </summary>
		public CspTerm TableDomain(CspTerm[] colVars, params CspDomain[][] inputs)
		{
			return Register(ConstraintContainer.TableDomain(colVars, inputs));
		}

		/// <summary>
		/// Add composite-wise constraints
		/// </summary>
		/// <param name="constraints">Constraints to be added. Must be constructed using Field Terms.</param>
		public bool AddConstraints(params CspTerm[] constraints)
		{
			if (_fVarCreated)
			{
				throw new InvalidOperationException(Resources.CompositeFroze + ToString());
			}
			return ConstraintContainer.AddConstraints(constraints);
		}

		/// <summary>
		/// Add a member to the composite.
		/// </summary>
		/// <param name="domain">DomainComposite of the field</param>
		/// <param name="key">The key by which we can refer to this field later</param>
		/// <param name="arity">The number of fields of this domain</param>
		public virtual CspTerm[] AddField(CspDomain domain, object key, int arity)
		{
			if (domain == null)
			{
				throw new ArgumentNullException(Resources.NullDomain);
			}
			if (_fVarCreated)
			{
				throw new InvalidOperationException(Resources.CompositeFroze + ToString());
			}
			if (arity <= 0)
			{
				throw new ArgumentException(Resources.CompositeFieldArityZero + ((key != null) ? key.ToString() : "null"));
			}
			if (_mapKeyTuple.ContainsKey(key))
			{
				throw new ArgumentException(Resources.CompositeDuplicateFields);
			}
			CspTerm[] array = ((domain != _baseConstraintSystem.DefaultBoolean) ? ConstraintContainer.CreateTemplateVariableVector(domain, key, arity) : ConstraintContainer.CreateTemplateBooleanVector(key, arity));
			ConstraintSystem.UpdateAllTermCount(BaseSolver, arity);
			Tuple tuple = new Tuple(key, arity, domain, array);
			_mapKeyTuple.Add(key, tuple);
			_fields.Add(tuple);
			CspTerm[] array2 = new CspTerm[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array2[i] = array[i];
			}
			return array2;
		}

		/// <summary>
		/// Access the field of the composite that has the given key.
		/// </summary>
		/// <param name="key"></param>
		/// <returns>An array of Terms that represent the fields. The length of the array is the same as the arity of this field.</returns>
		public IEnumerable<CspTerm> Fields(object key)
		{
			if (_fVarCreated)
			{
				throw new InvalidOperationException(Resources.CompositeFroze + ToString());
			}
			if (!_mapKeyTuple.ContainsKey(key))
			{
				throw new ArgumentException(Resources.CompositeInvalidFieldReference + ((key != null) ? key.ToString() : "null"));
			}
			_mapKeyTuple.TryGetValue(key, out var field);
			try
			{
				CspTerm[] vars = field.Vars;
				for (int i = 0; i < vars.Length; i++)
				{
					yield return vars[i];
				}
			}
			finally
			{
			}
		}

		/// <summary> String representation of this Composite
		/// </summary>
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (Key != this)
			{
				stringBuilder.Append(Key.ToString());
			}
			else
			{
				stringBuilder.Append("CompositeDomain");
			}
			stringBuilder.Append(" [");
			AppendTo(stringBuilder, 9).Append("]");
			return stringBuilder.ToString();
		}

		internal CspTerm Register(CspTerm term)
		{
			if (_fVarCreated)
			{
				throw new InvalidOperationException(Resources.CompositeFroze + ToString());
			}
			ConstraintSystem.UpdateAllTermCount(BaseSolver, 1);
			return term;
		}

		internal StringBuilder AppendTo(StringBuilder line, int itemLimit)
		{
			int num = 0;
			foreach (Tuple field in _fields)
			{
				if (num < itemLimit)
				{
					line.Append("(");
					line.Append(field.Key.ToString());
					line.Append(",");
					int arity = field.Arity;
					line.Append(arity.ToString(CultureInfo.InvariantCulture));
					line.Append(",");
					line.Append(field.Domain.ToString());
					line.Append("); ");
					continue;
				}
				break;
			}
			return line;
		}

		internal void Freeze()
		{
			_fVarCreated = true;
		}

		internal static CspComposite CreateComposite(ConstraintSystem solver, object key)
		{
			return new CspComposite(solver, key);
		}
	}
}
