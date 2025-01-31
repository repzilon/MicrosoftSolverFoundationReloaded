using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Properties;

namespace Microsoft.SolverFoundation.Common
{
	/// <summary> Maps from character location to (path, line, column). This is normally populated
	/// by filtering a token stream.
	/// </summary>
	internal class LineMapper
	{
		private readonly ITextVersion _tvr;

		private List<int> _rgich;

		/// <summary> default path
		/// </summary>
		public string DefaultPath => _tvr.Path;

		/// <summary> Maps from character location to (path, line, column). This is normally populated
		/// by filtering a token stream.
		/// </summary>
		/// <param name="tvr"> text version </param>
		public LineMapper(ITextVersion tvr)
		{
			_tvr = tvr;
			_rgich = new List<int>();
		}

		/// <summary> Add a new line
		/// </summary>
		/// <param name="tok"></param>
		public void AddNewLine(NewLineToken tok)
		{
			DebugContracts.NonNull(tok);
			if (!tok.GetSpan(_tvr, out var span))
			{
				throw new InvalidOperationException(Resources.BadTextVersionInNewLineToken);
			}
			int lim = span.Lim;
			if (_rgich.Count == 0 || _rgich[_rgich.Count - 1] < lim)
			{
				_rgich.Add(lim);
				return;
			}
			int num = FindIch(lim);
			if (0 >= num || _rgich[num - 1] != lim)
			{
				_rgich.Insert(num, lim);
			}
		}

		/// <summary> map a span to a position
		/// </summary>
		/// <param name="span"></param>
		/// <param name="spos"></param>
		/// <returns></returns>
		public bool MapSpanToPos(TextSpan span, out SrcPos spos)
		{
			spos = default(SrcPos);
			if (!_tvr.MapSpan(ref span))
			{
				return false;
			}
			spos.spanRaw = span;
			MapToLineCol(span.Min, out spos.pathMin, out spos.lineMin, out spos.colMin);
			MapToLineCol(span.Lim, out spos.pathLim, out spos.lineLim, out spos.colLim);
			return true;
		}

		/// <summary> Map to line and column
		/// </summary>
		/// <param name="ichSrc"></param>
		/// <param name="path"></param>
		/// <param name="line"></param>
		/// <param name="col"></param>
		private void MapToLineCol(int ichSrc, out string path, out int line, out int col)
		{
			int num = FindIch(ichSrc);
			if (num == 0)
			{
				line = 1;
				col = ichSrc + 1;
			}
			else
			{
				line = num + 1;
				col = ichSrc - _rgich[num - 1] + 1;
			}
			path = _tvr.Path;
		}

		private int FindIch(int ich)
		{
			int num = 0;
			int num2 = _rgich.Count;
			while (num < num2)
			{
				int num3 = (num + num2) / 2;
				if (_rgich[num3] <= ich)
				{
					num = num3 + 1;
				}
				else
				{
					num2 = num3;
				}
			}
			return num;
		}
	}
}
