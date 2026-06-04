using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	[System.Serializable]
	public enum BulletCurveMeasurementMode
	{
		Time,
		Distance
	}

	// A struct that holds data for curves, in order to make a parameter change through its lifetime.
	[System.Serializable]
	public struct BulletCurve
	{
		public bool enabled;
		public WrapMode wrapMode;
		[Tooltip("Time: parameter evolves naturally over time.\nDistance: parameter evolves along bullet movement.")]
		public BulletCurveMeasurementMode measurementMode;
		public bool periodIsLifespan;
		public AnimationCurve curve;

		public float timeSinceLive { get; private set; }
		public Bullet relatedBullet { get; private set; }

		[SerializeField] float _period;
		public float period
		{
			get { return _period; }
			set
			{
				float v = value;
				if (v <= 0) v = 0.1f;
				_period = v;
				invPeriod = 1/v;
			}
		}

		public float invPeriod { get; private set; } // equals 1/period, to avoid divisions at Update
		
		// lifespan of bullet or emitter that uses this curve :
		public float lifespan { get; private set; }
		public float invLifespan { get; private set; }

		// max travellable distance of bullet or emitter that uses this curve :
		public float travellableDist { get; private set; }
		public float invTravellableDist { get; private set; }
		
		public bool isPlaying { get; private set; }
		public bool isBackwards { get; private set; }

		// Looks at timeSinceLive and WrapMode, then returns wanted current curve time between 0 and 1.
		public float GetRatio()
		{
			// compute the period: either the explicit one, or one computed from the related bullet (time or distance)
			float usedPeriod = period;
			float usedInvPeriod = invPeriod;
			if (periodIsLifespan)
			{
				if (measurementMode == BulletCurveMeasurementMode.Time)
				{
					usedPeriod = lifespan;
					usedInvPeriod = invLifespan;
				}
				else if (measurementMode == BulletCurveMeasurementMode.Distance)
				{
					usedPeriod = travellableDist;
					usedInvPeriod = invTravellableDist;
				}
			}

			// compute curve abscissa: either "timeSinceLive" (for time) or, if applicable, related bullet's travelled distance
			float curveX = timeSinceLive;
			if (measurementMode == BulletCurveMeasurementMode.Distance && (relatedBullet != null))
				curveX = relatedBullet.moduleMovement.totalTravelledDistance;

			
			float t = 0;

			if (wrapMode == WrapMode.Loop) t = (curveX % usedPeriod) * usedInvPeriod;
			else if (wrapMode == WrapMode.PingPong)
			{
				t = (curveX % (2 * usedPeriod)) * usedInvPeriod * 0.5f;
				t = 1 - 2 * Mathf.Abs(t - 0.5f);
			}
			else t = Mathf.Min(curveX * usedInvPeriod, 1);
			return t;
		}

		// Evaluate Curves to figure out actual current parameter values
		public float GetCurveResult()
		{
			return curve.Evaluate(GetRatio());
		}

		// Called every frame when Bullet Modules are updated
		public void Update() => Update(Time.deltaTime);

		// Called for update with user-defined timestep
		public void Update(float timestep)
		{
			if (!isPlaying) return;
			
			if (isBackwards) timeSinceLive -= timestep;
			else timeSinceLive += timestep;
		}

		// Main control functions
		public void Play() { isPlaying = true; isBackwards = false; }
		public void Rewind() { isPlaying = true; isBackwards = true; }
		public void Pause() { isPlaying = false; }
		public void Toggle() { isPlaying = !isPlaying; }
		public void Reset() { timeSinceLive = 0f; }
		public void Stop() { Pause(); Reset(); }
		public void Boot() { Reset(); Play(); }
		public void SetRawTime(float newValue) { timeSinceLive = newValue; }
		public void SetRatio(float newRatio) { SetRawTime(period * newRatio); }

		// Called at ApplyBulletParams, and upon modifying bullet lifetime
		public void UpdateInternalValues(Bullet bullet)
		{
			if (bullet)
			{
				relatedBullet = bullet;
				if (bullet.moduleLifespan.isEnabled)
				{
					lifespan = bullet.moduleLifespan.lifespan;
					travellableDist = bullet.moduleLifespan.maxTravellableDistance;
				}
			}

			if (lifespan <= 0)
				lifespan = 0.1f;

			if (travellableDist <= 0)
				travellableDist = 0.1f;
			
			UpdateInternalValues();
		}

		// Overload independent from bullet informations, called by its bullet-dependent part
		public void UpdateInternalValues()
		{
			invLifespan = (lifespan > 0) ? (1 / lifespan) : 10f;
			invPeriod = (period > 0) ? (1 / period) : 10f;
			invTravellableDist = (travellableDist > 0) ? (1 / travellableDist) : 10f;
			//timeSinceLive = 0; // 2020-08-25 : not needed anymore, also it blocked some actions
		}
	}

	// A struct where every parameter is the Dynamic counterpart of regular BulletCurves. Solved upon bullet emission. Currently unused.
	[System.Serializable]
	public struct DynamicBulletCurve
	{
		public bool enabled;
		public DynamicEnum wrapMode;
		public DynamicEnum measurementMode;
		public DynamicBool periodIsLifespan;
		public DynamicFloat period;
		public DynamicAnimationCurve curve;

		public DynamicBulletCurve(bool _forceZeroToOne)
		{
			enabled = false;
			wrapMode = new DynamicEnum((int)WrapMode.Default);
			measurementMode = new DynamicEnum((int)BulletCurveMeasurementMode.Time);
			periodIsLifespan = new DynamicBool(false);
			period = new DynamicFloat(1.0f);
			curve = new DynamicAnimationCurve(AnimationCurve.EaseInOut(0,0,1,1));
			#if UNITY_EDITOR
			wrapMode.SetEnumType(typeof(WrapMode));
			measurementMode.SetEnumType(typeof(BulletCurveMeasurementMode));
			curve.SetForceZeroToOne(_forceZeroToOne);
			#endif
		}
	}
}