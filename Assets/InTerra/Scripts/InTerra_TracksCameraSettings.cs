using UnityEngine;

#if USING_HDRP
	using UnityEngine.Rendering.HighDefinition;
#endif
#if USING_URP
	using UnityEngine.Rendering.Universal;
#endif


namespace InTerra
{
	public class InTerra_TracksCameraSettings 
	{
		public static void SetTrackCamera(Camera trackCamera)
		{
			var updaterScript = InTerra_Data.GetUpdaterScript();

			trackCamera.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
			trackCamera.clearFlags = CameraClearFlags.Color;
			trackCamera.backgroundColor = new Color(0, 0, 0, 0);
			trackCamera.cullingMask = 1 << updaterScript.TrackLayer;
			trackCamera.orthographic = true;
			trackCamera.orthographicSize = updaterScript.TrackArea / 2;
			if (updaterScript.TrackTexture == null)
			{
				InTerra_Data.CreateTrackRenderTexture();
			}
			trackCamera.forceIntoRenderTexture = true;
			trackCamera.targetTexture = updaterScript.TrackTexture;

			trackCamera.cullingMask = 1 << updaterScript.TrackLayer;
			trackCamera.allowHDR = false;
			trackCamera.allowDynamicResolution = false;
			trackCamera.targetTexture = updaterScript.TrackTexture;
			
			
			#if USING_HDRP

				if (!trackCamera.TryGetComponent<HDAdditionalCameraData>(out HDAdditionalCameraData hd))
				{
					InTerra_Data.updater.AddComponent<HDAdditionalCameraData>();
				}

				var hdAdditionalCameraData = trackCamera.GetComponent<HDAdditionalCameraData>();

				hdAdditionalCameraData.clearColorMode = new HDAdditionalCameraData.ClearColorMode();
				hdAdditionalCameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
				hdAdditionalCameraData.backgroundColorHDR = new Color(0, 0, 0, 0);
				hdAdditionalCameraData.clearDepth = false;
				hdAdditionalCameraData.volumeLayerMask = 0;				
				hdAdditionalCameraData.probeLayerMask = 0;
			
				hdAdditionalCameraData.customRenderingSettings = true;

				var fsm = hdAdditionalCameraData.renderingPathCustomFrameSettingsOverrideMask.mask;
				var fs = hdAdditionalCameraData.renderingPathCustomFrameSettings;

				fsm[(uint)FrameSettingsField.LitShaderMode] = true;
				fsm[(uint)FrameSettingsField.MSAAMode] = true;
				fsm[(uint)FrameSettingsField.Decals] = true;
				fsm[(uint)FrameSettingsField.TransparentPrepass] = true;
				fsm[(uint)FrameSettingsField.TransparentPostpass] = true;
				fsm[(uint)FrameSettingsField.RayTracing] = true;
				fsm[(uint)FrameSettingsField.CustomPass] = true;
				fsm[(uint)FrameSettingsField.MotionVectors] = true;
				fsm[(uint)FrameSettingsField.Refraction] = true;
				fsm[(uint)FrameSettingsField.Distortion] = true;
				fsm[(uint)FrameSettingsField.Postprocess] = true;
				fsm[(uint)FrameSettingsField.CustomPass] = true;
				fsm[(uint)FrameSettingsField.AfterPostprocess] = true;

				fsm[(uint)FrameSettingsField.ShadowMaps] = true;
				fsm[(uint)FrameSettingsField.ContactShadows] = true;
				fsm[(uint)FrameSettingsField.ScreenSpaceShadows] = true;
				fsm[(uint)FrameSettingsField.Shadowmask] = true;
				fsm[(uint)FrameSettingsField.SSR] = true;
				fsm[(uint)FrameSettingsField.SSGI] = true;
				fsm[(uint)FrameSettingsField.SSAO] = true;
				fsm[(uint)FrameSettingsField.Transmission] = true;
				fsm[(uint)FrameSettingsField.AtmosphericScattering] = true;
				fsm[(uint)FrameSettingsField.LightLayers] = true;
				fsm[(uint)FrameSettingsField.ExposureControl] = true;
				fsm[(uint)FrameSettingsField.ReflectionProbe] = true;
				fsm[(uint)FrameSettingsField.PlanarProbe] = true;
				fsm[(uint)FrameSettingsField.ReplaceDiffuseForIndirect] = true;
				fsm[(uint)FrameSettingsField.SkyReflection] = true;
				fsm[(uint)FrameSettingsField.DirectSpecularLighting] = true;
				fsm[(uint)FrameSettingsField.SubsurfaceScattering] = true;
				fsm[(uint)FrameSettingsField.FullResolutionCloudsForSky] = true;
				fsm[(uint)FrameSettingsField.VolumetricClouds] = true;
				fsm[(uint)FrameSettingsField.AsyncCompute] = true;
				fsm[(uint)FrameSettingsField.FPTLForForwardOpaque] = true;
				fsm[(uint)FrameSettingsField.BigTilePrepass] = true;
				fsm[(uint)FrameSettingsField.DeferredTile] = true;
				fsm[(uint)FrameSettingsField.ComputeLightEvaluation] = true;
				fsm[(uint)FrameSettingsField.ComputeLightVariants] = true;
				fsm[(uint)FrameSettingsField.ComputeMaterialVariants] = true;
				
				bool setFSetting = false;

				fs.litShaderMode = LitShaderMode.Forward;
				fs.SetEnabled(FrameSettingsField.LitShaderMode, setFSetting);
				fs.SetEnabled(FrameSettingsField.MSAAMode, setFSetting);
				fs.SetEnabled(FrameSettingsField.Decals, setFSetting);
				fs.SetEnabled(FrameSettingsField.TransparentPrepass, setFSetting);
				fs.SetEnabled(FrameSettingsField.TransparentPostpass, setFSetting);
				fs.SetEnabled(FrameSettingsField.RayTracing, setFSetting);
				fs.SetEnabled(FrameSettingsField.CustomPass, setFSetting);
				fs.SetEnabled(FrameSettingsField.MotionVectors, setFSetting);
				fs.SetEnabled(FrameSettingsField.Refraction, setFSetting);
				fs.SetEnabled(FrameSettingsField.Distortion, setFSetting);
				fs.SetEnabled(FrameSettingsField.Postprocess, setFSetting);
				fs.SetEnabled(FrameSettingsField.CustomPass, setFSetting);
				fs.SetEnabled(FrameSettingsField.AfterPostprocess, setFSetting);

				fs.SetEnabled(FrameSettingsField.ShadowMaps, setFSetting);
				fs.SetEnabled(FrameSettingsField.ContactShadows, setFSetting);
				fs.SetEnabled(FrameSettingsField.ScreenSpaceShadows, setFSetting);
				fs.SetEnabled(FrameSettingsField.Shadowmask, setFSetting);
				fs.SetEnabled(FrameSettingsField.SSR, setFSetting);
				fs.SetEnabled(FrameSettingsField.SSGI, setFSetting);
				fs.SetEnabled(FrameSettingsField.SSAO, setFSetting);
				fs.SetEnabled(FrameSettingsField.Transmission, setFSetting);
				fs.SetEnabled(FrameSettingsField.AtmosphericScattering, setFSetting);
				fs.SetEnabled(FrameSettingsField.LightLayers, setFSetting);
				fs.SetEnabled(FrameSettingsField.ExposureControl, setFSetting);
				fs.SetEnabled(FrameSettingsField.ReflectionProbe, setFSetting);
				fs.SetEnabled(FrameSettingsField.PlanarProbe, setFSetting);
				fs.SetEnabled(FrameSettingsField.ReplaceDiffuseForIndirect, setFSetting);
				fs.SetEnabled(FrameSettingsField.SkyReflection, setFSetting);
				fs.SetEnabled(FrameSettingsField.DirectSpecularLighting, setFSetting);
				fs.SetEnabled(FrameSettingsField.SubsurfaceScattering, setFSetting);
				fs.SetEnabled(FrameSettingsField.FullResolutionCloudsForSky, setFSetting);
				fs.SetEnabled(FrameSettingsField.VolumetricClouds, setFSetting);
				fs.SetEnabled(FrameSettingsField.AsyncCompute, setFSetting);
				fs.SetEnabled(FrameSettingsField.FPTLForForwardOpaque, setFSetting);
				fs.SetEnabled(FrameSettingsField.BigTilePrepass, setFSetting);
				fs.SetEnabled(FrameSettingsField.DeferredTile, setFSetting);
				fs.SetEnabled(FrameSettingsField.ComputeLightEvaluation, setFSetting);
				fs.SetEnabled(FrameSettingsField.ComputeLightVariants, setFSetting);
				fs.SetEnabled(FrameSettingsField.ComputeMaterialVariants, setFSetting);
				fs.SetEnabled(FrameSettingsField.CustomPass, true);

			
			

			hdAdditionalCameraData.renderingPathCustomFrameSettingsOverrideMask.mask = fsm;
			hdAdditionalCameraData.renderingPathCustomFrameSettings = fs;
		#endif
		
			
		#if USING_URP
			if (!trackCamera.TryGetComponent<UniversalAdditionalCameraData>(out UniversalAdditionalCameraData c))
			{
				InTerra_Data.updater.AddComponent<UniversalAdditionalCameraData>();
			}

			var additionalCameraData = trackCamera.GetComponent<UniversalAdditionalCameraData>();

			additionalCameraData.antialiasing = AntialiasingMode.None;
			additionalCameraData.volumeLayerMask = 0;
			additionalCameraData.renderShadows = false;
			additionalCameraData.renderPostProcessing = false;
			additionalCameraData.renderType = CameraRenderType.Base;
		#endif
		trackCamera.enabled = true;
		}
	}
}


