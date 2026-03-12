using DunGen.Graph;
using System;
using System.Text;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace DunGen.Analysis
{
	public delegate void RuntimeAnalyzerDelegate(RuntimeAnalyzer analyzer);
	public delegate void AnalysisUpdatedDelegate(RuntimeAnalyzer analyzer, GenerationAnalysis analysis, GenerationStats generationStats, int currentIteration, int totalIterations);

	[AddComponentMenu("DunGen/Analysis/Runtime Analyzer")]
	public sealed class RuntimeAnalyzer : MonoBehaviour
	{
		#region Nested Types

		public enum SeedMode
		{
			Random,
			Incremental,
			Fixed,
		}

		#endregion

		public static event RuntimeAnalyzerDelegate AnalysisStarted;
		public static event RuntimeAnalyzerDelegate AnalysisComplete;
		public static event AnalysisUpdatedDelegate AnalysisUpdated;

		public DungeonFlow DungeonFlow;
		public int Iterations = 100;
		public int MaxFailedAttempts = 20;
		public bool RunOnStart = true;
		public float MaximumAnalysisTime = 0;
		public SeedMode SeedGenerationMode = SeedMode.Random;
		public int Seed = 0;
		public bool ClearDungeonOnCompletion = true;
		public bool AllowTilePooling = false;
		public int CurrentIterations { get { return targetIterations - remainingIterations; } }

		private DungeonGenerator generator = new DungeonGenerator();
		private GenerationAnalysis analysis;
		private readonly StringBuilder infoText = new StringBuilder();
		private bool finishedEarly;
		private bool prevShouldRandomizeSeed;
		private int targetIterations;

		private int remainingIterations;
		private Stopwatch analysisTime;
		private bool generateNextFrame;
		private int currentSeed;
		private RandomStream randomStream;


		private void Start()
		{
			if (RunOnStart)
				RunAnalysis();
		}

		[Obsolete("Use RunAnalysis() instead")]
		public void Analyze() => RunAnalysis();

		public void RunAnalysis()
		{
			bool isValid = false;

			if (DungeonFlow == null)
				Debug.LogError("No DungeonFlow assigned to analyser");
			else if (Iterations <= 0)
				Debug.LogError("Iteration count must be greater than 0");
			else if (MaxFailedAttempts <= 0)
				Debug.LogError("Max failed attempt count must be greater than 0");
			else
				isValid = true;

			if (!isValid)
				return;

			AnalysisStarted?.Invoke(this);
			prevShouldRandomizeSeed = generator.ShouldRandomizeSeed;

			generator.IsAnalysis = true;
			generator.DungeonFlow = DungeonFlow;
			generator.MaxAttemptCount = MaxFailedAttempts;
			generator.ShouldRandomizeSeed = false;
			generator.AllowTilePooling = AllowTilePooling;

			analysis = new GenerationAnalysis(Iterations);
			analysisTime = Stopwatch.StartNew();
			remainingIterations = targetIterations = Iterations;

			randomStream = new RandomStream(Seed);

			generator.OnGenerationStatusChanged += OnGenerationStatusChanged;
			GenerateNext();
		}

		private void GenerateNext()
		{
			switch(SeedGenerationMode)
			{
				case SeedMode.Random:
					currentSeed = randomStream.Next();
					break;
				case SeedMode.Incremental:
					currentSeed++;
					break;
				case SeedMode.Fixed:
					currentSeed = Seed;
					break;
			}

			generator.Seed = currentSeed;
			generator.Generate();
		}

		private void Update()
		{
			if (MaximumAnalysisTime > 0 && analysisTime.Elapsed.TotalSeconds >= MaximumAnalysisTime)
			{
				remainingIterations = 0;
				finishedEarly = true;
			}

			if (generateNextFrame)
			{
				generateNextFrame = false;
				GenerateNext();
			}
		}

		private void CompleteAnalysis()
		{
			analysisTime.Stop();
			analysis.CalculateMetrics();

			if(ClearDungeonOnCompletion)
				UnityUtil.Destroy(generator.Root);

			OnAnalysisComplete();
			AnalysisComplete?.Invoke(this);
		}

		private void OnGenerationStatusChanged(DungeonGenerator generator, GenerationStatus status)
		{
			if(status != GenerationStatus.Complete && status != GenerationStatus.Failed)
				return;

			bool success = status == GenerationStatus.Complete;

			if (success)
			{
				analysis.IncrementSuccessCount();
				analysis.Add(generator.GenerationStats);
			}

			AnalysisUpdated?.Invoke(this, analysis, generator.GenerationStats, CurrentIterations, targetIterations);

			remainingIterations--;

			if (remainingIterations <= 0)
			{
				generator.OnGenerationStatusChanged -= OnGenerationStatusChanged;
				CompleteAnalysis();
			}
			else
				generateNextFrame = true;
		}

		private void OnAnalysisComplete()
		{
			const int textPadding = 20;

			void AddInfoEntry(StringBuilder stringBuilder, string title, NumberSetData data)
			{
				string spacing = new string(' ', textPadding - title.Length);
				stringBuilder.Append($"\n\t{title}:{spacing}\t{data}");
			}

			generator.ShouldRandomizeSeed = prevShouldRandomizeSeed;
			infoText.Length = 0;

			if (finishedEarly)
				infoText.AppendLine("[ Reached maximum analysis time before the target number of iterations was reached ]");

			infoText.AppendFormat("Iterations: {0}, Max Failed Attempts: {1}", (finishedEarly) ? analysis.IterationCount : analysis.TargetIterationCount, MaxFailedAttempts);
			infoText.AppendFormat("\nTotal Analysis Time: {0:0.00} seconds", analysisTime.Elapsed.TotalSeconds);
			//infoText.AppendFormat("\n\tOf which spent generating dungeons: {0:0.00} seconds", analysis.AnalysisTime / 1000.0f);
			infoText.AppendFormat("\nDungeons successfully generated: {0}% ({1} failed)", Mathf.RoundToInt(analysis.SuccessPercentage), analysis.TargetIterationCount - analysis.SuccessCount);

			infoText.AppendLine();
			infoText.AppendLine();

			infoText.Append("## TIME TAKEN (in milliseconds) ##");

			foreach (var step in GenerationAnalysis.MeasurableSteps)
				AddInfoEntry(infoText, step.ToString(), analysis.GetGenerationStepData(step));

			infoText.Append("\n\t-------------------------------------------------------");
			AddInfoEntry(infoText, "Total", analysis.TotalTime);

			infoText.AppendLine();
			infoText.AppendLine();

			infoText.AppendLine("## ROOM DATA ##");
			AddInfoEntry(infoText, "Main Path Rooms", analysis.MainPathRoomCount);
			AddInfoEntry(infoText, "Branch Path Rooms", analysis.BranchPathRoomCount);
			infoText.Append("\n\t-------------------");
			AddInfoEntry(infoText, "Total", analysis.TotalRoomCount);

			infoText.AppendLine();
			infoText.AppendLine();

			infoText.AppendFormat("Retry Count: {0}", analysis.TotalRetries);
		}

		private void OnGUI()
		{
			if (analysis == null || infoText == null || infoText.Length == 0)
			{
				string failedGenerationsCountText = (analysis.SuccessCount < analysis.IterationCount) ? ("\nFailed Dungeons: " + (analysis.IterationCount - analysis.SuccessCount).ToString()) : "";

				GUILayout.Label(string.Format("Analysing... {0} / {1} ({2:0.0}%){3}", CurrentIterations, targetIterations, (CurrentIterations / (float)targetIterations) * 100, failedGenerationsCountText));
				return;
			}

			GUILayout.Label(infoText.ToString());
		}
	}
}