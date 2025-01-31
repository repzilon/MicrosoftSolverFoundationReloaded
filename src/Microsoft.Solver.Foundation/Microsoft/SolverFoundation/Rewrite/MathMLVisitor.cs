using System;
using System.Collections.Generic;
using System.Xml;

namespace Microsoft.SolverFoundation.Rewrite
{
	/// <summary>
	/// Implements a visitor which produces MathML.  
	/// </summary>
	internal class MathMLVisitor : Visitor
	{
		private enum State
		{
			Model,
			Variables,
			Domain,
			Constraints,
			Objective
		}

		private const string MathMLPrefix = "mml";

		private const string MathMLNamespace = "http://www.w3.org/1998/Math/MathML";

		private State _state;

		private bool _needsMtrEnd;

		private bool _varInteger;

		private Stack<string> _operStack = new Stack<string>();

		private XmlWriter _writer;

		private int _depth;

		private bool _isAbs;

		private bool _isPower;

		private bool _addHtmlBoilerplate;

		internal MathMLVisitor(XmlWriter xw)
		{
			_needsMtrEnd = false;
			_writer = xw;
			_writer.WriteStartDocument();
			if (_addHtmlBoilerplate)
			{
				_writer.WriteStartElement("html");
				_writer.WriteStartElement("object");
				_writer.WriteAttributeString("id", null, "showEqn");
				_writer.WriteAttributeString("classId", null, "clsid:32F66A20-7614-11D4-BD11-00104BD3F987");
				_writer.WriteString(" ");
				_writer.WriteEndElement();
				_writer.WriteRaw("<?import NAMESPACE=\"MML\" IMPLEMENTATION=\"#showEqn\"?>\n");
			}
			_writer.WriteStartElement("mml", "math", "http://www.w3.org/1998/Math/MathML");
			_writer.WriteAttributeString("xmlns", "mml", null, "http://www.w3.org/1998/Math/MathML");
			_writer.WriteStartElement("mml", "mtable", "http://www.w3.org/1998/Math/MathML");
			_depth = 0;
			_state = State.Model;
		}

		private void DumpVariable(string varName, string lower, string upper, bool isInteger)
		{
			_writer.WriteElementString("mml", "mi", "http://www.w3.org/1998/Math/MathML", varName);
			_writer.WriteStartElement("mml", "mo", "http://www.w3.org/1998/Math/MathML");
			_writer.WriteRaw(" &#x03F5; ");
			_writer.WriteEndElement();
			_writer.WriteStartElement("mml", "mi", "http://www.w3.org/1998/Math/MathML");
			if (isInteger)
			{
				_writer.WriteRaw(" &#x2124; ");
			}
			else
			{
				_writer.WriteRaw(" &#x211D; ");
			}
			_writer.WriteEndElement();
			_writer.WriteStartElement("mml", "mo", "http://www.w3.org/1998/Math/MathML");
			_writer.WriteRaw("|");
			_writer.WriteEndElement();
			if (lower == "-Infinity")
			{
				_writer.WriteStartElement("mml", "mi", "http://www.w3.org/1998/Math/MathML");
				_writer.WriteRaw(" -&#x221E; ");
				_writer.WriteEndElement();
			}
			else
			{
				_writer.WriteElementString("mml", "mi", "http://www.w3.org/1998/Math/MathML", lower);
			}
			_writer.WriteStartElement("mml", "mo", "http://www.w3.org/1998/Math/MathML");
			_writer.WriteRaw(" &#x2264; ");
			_writer.WriteEndElement();
			_writer.WriteElementString("mml", "mi", "http://www.w3.org/1998/Math/MathML", varName);
			_writer.WriteStartElement("mml", "mo", "http://www.w3.org/1998/Math/MathML");
			_writer.WriteRaw(" &#x2264; ");
			_writer.WriteEndElement();
			if (upper == "Infinity")
			{
				_writer.WriteStartElement("mml", "mi", "http://www.w3.org/1998/Math/MathML");
				_writer.WriteRaw(" &#x221E; ");
				_writer.WriteEndElement();
			}
			else
			{
				_writer.WriteElementString("mml", "mi", "http://www.w3.org/1998/Math/MathML", upper);
			}
			_needsMtrEnd = true;
		}

		private void DumpBooleanVariable(string varName)
		{
			_writer.WriteElementString("mml", "mi", "http://www.w3.org/1998/Math/MathML", varName);
			_writer.WriteStartElement("mml", "mo", "http://www.w3.org/1998/Math/MathML");
			_writer.WriteRaw(" &#x03F5; ");
			_writer.WriteEndElement();
			_writer.WriteStartElement("mml", "mfenced", "http://www.w3.org/1998/Math/MathML");
			_writer.WriteAttributeString("open", "{");
			_writer.WriteAttributeString("close", "}");
			_writer.WriteElementString("mml", "mn", "http://www.w3.org/1998/Math/MathML", "0");
			_writer.WriteElementString("mml", "mn", "http://www.w3.org/1998/Math/MathML", "1");
			_writer.WriteEndElement();
			_needsMtrEnd = true;
		}

		private bool DumpConstraint(Expression constraint)
		{
			_state = State.Constraints;
			for (int i = 1; i < constraint.Arity; i++)
			{
				Expression expr = constraint[i];
				if (!Visit(expr))
				{
					return false;
				}
			}
			return true;
		}

		private bool DumpObjective(Expression goal)
		{
			for (int i = 1; i < goal.Arity; i++)
			{
				Expression expr = goal[i];
				if (!Visit(expr))
				{
					return false;
				}
				_needsMtrEnd = true;
			}
			return true;
		}

		/// <summary>
		/// Main entry point for visiting a model and producing MathML output.
		/// </summary>
		/// <param name="expr"></param>
		/// <returns></returns>
		public override bool Visit(Expression expr)
		{
			bool flag = false;
			bool flag2 = false;
			if (expr is Constant c)
			{
				return VisitConstant(c);
			}
			if (expr is Symbol sym)
			{
				bool result = VisitSymbol(sym);
				if (_isAbs)
				{
					_writer.WriteStartElement("mml", "mo", "http://www.w3.org/1998/Math/MathML");
					_writer.WriteRaw("|");
					_writer.WriteEndElement();
				}
				return result;
			}
			if (!(expr is Invocation invocation))
			{
				return VisitOther(expr);
			}
			if (!PreVisitInvocation(invocation))
			{
				return false;
			}
			if (invocation.Head is DecisionsSymbol)
			{
				_state = State.Domain;
			}
			else
			{
				if (invocation.Head is ConstraintsSymbol)
				{
					bool flag3 = true;
					for (int i = 0; i < invocation.Arity; i++)
					{
						if (!(invocation[i].Head is RuleSymbol))
						{
							continue;
						}
						DumpConstraint(invocation[i]);
						if (i + 1 != invocation.Arity)
						{
							_needsMtrEnd = true;
							if (!PostVisitInvocation(invocation) || !PreVisitInvocation(invocation))
							{
								break;
							}
						}
					}
					return PostVisitInvocation(invocation);
				}
				if (invocation.Head is MinimizeSymbol || invocation.Head is MaximizeSymbol)
				{
					_state = State.Objective;
					_writer.WriteElementString("mml", "mtext", "http://www.w3.org/1998/Math/MathML", invocation.Head.ToString());
					DumpObjective(invocation[0]);
					return PostVisitInvocation(invocation);
				}
				if ((_state == State.Constraints || _state == State.Objective) && invocation.Arity == 1)
				{
					if (!(invocation.Head is Symbol) || !(invocation.Head.GetType() != typeof(Symbol)))
					{
						_writer.WriteElementString("mml", "mi", "http://www.w3.org/1998/Math/MathML", invocation.ToString());
						return PostVisitInvocation(invocation);
					}
					if (!Visit(invocation.Head))
					{
						return false;
					}
					if (_isAbs)
					{
						flag = true;
						_isAbs = false;
					}
				}
				else if (!Visit(invocation.Head))
				{
					return false;
				}
			}
			if (_isPower)
			{
				flag2 = true;
				_isPower = false;
			}
			string text = string.Empty;
			if (_state == State.Domain && invocation.Arity >= 2)
			{
				Expression expression = invocation[0];
				_varInteger = expression.Head is IntegersSymbol;
				for (int j = 1; j < invocation.Arity; j++)
				{
					if (expression is BooleansSymbol)
					{
						DumpBooleanVariable(invocation[j].ToString());
					}
					else
					{
						DumpVariable(invocation[j].ToString(), expression[0].ToString(), expression[1].ToString(), _varInteger);
					}
					if (j + 1 < invocation.Arity)
					{
						_writer.WriteEndElement();
						_writer.WriteEndElement();
						_writer.WriteStartElement("mml", "mtr", "http://www.w3.org/1998/Math/MathML");
						_writer.WriteStartElement("mml", "mrow", "http://www.w3.org/1998/Math/MathML");
						_needsMtrEnd = false;
					}
				}
				return PostVisitInvocation(invocation);
			}
			if (flag2)
			{
				_writer.WriteStartElement("mml", "msup", "http://www.w3.org/1998/Math/MathML");
			}
			for (int k = 0; k < invocation.Arity; k++)
			{
				if (k > 0 && (_state == State.Constraints || _state == State.Objective))
				{
					if (!flag2 && _operStack.Count > 0 && k == 1)
					{
						text = _operStack.Pop();
					}
					if (!flag2 && !string.IsNullOrEmpty(text))
					{
						_writer.WriteStartElement("mml", "mo", "http://www.w3.org/1998/Math/MathML");
						_writer.WriteRaw(text);
						_writer.WriteEndElement();
					}
				}
				if (!Visit(invocation[k]))
				{
					return false;
				}
			}
			if (flag)
			{
				_writer.WriteStartElement("mml", "mo", "http://www.w3.org/1998/Math/MathML");
				_writer.WriteRaw("|");
				_writer.WriteEndElement();
			}
			if (flag2)
			{
				_writer.WriteEndElement();
			}
			return PostVisitInvocation(invocation);
		}

		public override bool VisitConstant(Constant c)
		{
			_writer.WriteStartElement("mml", "mn", "http://www.w3.org/1998/Math/MathML");
			_writer.WriteString(c.ToString());
			_writer.WriteEndElement();
			return true;
		}

		public override bool VisitSymbol(Symbol sym)
		{
			_isAbs = false;
			_isPower = false;
			switch (_state)
			{
			case State.Domain:
			{
				RootSymbol rootSymbol = sym.Head as RootSymbol;
				if (rootSymbol == null)
				{
					throw new NotImplementedException();
				}
				break;
			}
			case State.Model:
				if (!(sym is ModelSymbol))
				{
					throw new InvalidOperationException();
				}
				_state = State.Variables;
				break;
			case State.Variables:
				_state = State.Domain;
				return true;
			case State.Constraints:
			case State.Objective:
				if (sym is PlusSymbol || sym is TimesSymbol || sym is EqualSymbol)
				{
					_operStack.Push(sym.ParseInfo.OperatorText);
				}
				else if (sym is LessEqualSymbol)
				{
					_operStack.Push("&#x2264;");
				}
				else if (sym is AbsSymbol)
				{
					_isAbs = true;
				}
				else if (sym is GreaterEqualSymbol)
				{
					_operStack.Push("&#x2265;");
				}
				else if (sym is UnequalSymbol)
				{
					_operStack.Push("&#x2260;");
				}
				else if (sym is PowerSymbol)
				{
					_isPower = true;
				}
				else
				{
					_writer.WriteElementString("mml", "mi", "http://www.w3.org/1998/Math/MathML", sym.ToString());
				}
				break;
			default:
				throw new NotImplementedException();
			}
			return true;
		}

		public override bool PreVisitInvocation(Invocation inv)
		{
			_depth++;
			if (_depth == 1)
			{
				_writer.WriteStartElement("mml", "mtr", "http://www.w3.org/1998/Math/MathML");
			}
			else if (_depth > 1)
			{
				_writer.WriteStartElement("mml", "mrow", "http://www.w3.org/1998/Math/MathML");
			}
			return true;
		}

		public override bool PostVisitInvocation(Invocation inv)
		{
			_depth--;
			_writer.WriteEndElement();
			if (_needsMtrEnd)
			{
				_needsMtrEnd = false;
				_writer.WriteEndElement();
				_writer.WriteStartElement("mml", "mtr", "http://www.w3.org/1998/Math/MathML");
			}
			else if (_depth == 0)
			{
				_writer.WriteEndElement();
				_writer.WriteEndElement();
				_writer.WriteEndDocument();
			}
			return true;
		}

		public override bool VisitOther(Expression expr)
		{
			throw new NotImplementedException();
		}
	}
}
