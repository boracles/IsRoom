Shader "Custom/CustomDecal" 
{

	Properties 
	{
		_MainTex("Base (RGB) Opacity (A)", 2D) = "white" {}
		_Color("Overlay Color", Color) = (1,1,1,1)
		_Glossiness("Smoothness Map", 2D) = "white" {}
		_Gloss("Smoothness", Range(0.0, 1.0)) = 0.5
		_BumpMap("Normal Map", 2D) = "bump" {}
		_BumpScale("Normal Strength", Range(0.0, 5.0)) = 1.0
		_Metallic("Metallic", Range(0.0, 1.0)) = 0.0
	}

	SubShader 
	{

		Tags { "RenderType"="Transparent" "IgnoreProjector" = "True" "Queue" = "AlphaTest" }
		LOD 200
		

		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows decal:blend
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _BumpMap;
		sampler2D _Glossiness;
		float _Gloss, _Metallic, _BumpScale;
		fixed4 _Color;

		struct Input 
		{
			float2 uv_MainTex;
			float2 uv_BumpMap;
			float4 color: Color;
		};

		void surf (Input IN, inout SurfaceOutputStandard o) 
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color  * IN.color;
			o.Albedo = c.rgb;
			o.Alpha = c.a;
			o.Normal = UnpackScaleNormal(tex2D(_BumpMap, IN.uv_BumpMap), _BumpScale);
			half4 glossMap = tex2D(_Glossiness, IN.uv_MainTex);
			o.Metallic = _Metallic;
			o.Smoothness = glossMap.r * _Gloss;
		}

		ENDCG
	}

	FallBack "Decal/Transparent Diffuse"
}