using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DunGen.Analysis
{
	public sealed class NumberSetData
	{
		public float? Min { get; private set; }
		public float? Max { get; private set; }
		public float? Average { get; private set; }
		public float? StandardDeviation { get; private set; }


		public NumberSetData(IEnumerable<float> values)
		{
			if (!values.Any())
			{
				Min = null;
				Max = null;
				Average = null;
				StandardDeviation = null;

				return;
			}

			var valuesArray = values.ToArray();

			Min = valuesArray.Min();
			Max = valuesArray.Max();
			Average = valuesArray.Average();

			// Calculate standard deviation
			float[] temp = new float[valuesArray.Length];

			for (int i = 0; i < temp.Length; i++)
				temp[i] = Mathf.Pow(valuesArray[i] - Average.Value, 2);

			StandardDeviation = Mathf.Sqrt(temp.Sum() / temp.Length);
		}

		public override string ToString()
		{
			if(!Min.HasValue)
				return "[ No data available ]";
			else
				return string.Format("[ Min: {0:N1}, Max: {1:N1}, Average: {2:N1}, Standard Deviation: {3:N2} ]", Min, Max, Average, StandardDeviation);
		}
	}
}

