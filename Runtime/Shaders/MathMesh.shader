Shader "MathMesher/Vertex Color"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _SideShade ("Side shading", Range(0,1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Back
        ZWrite On
        CGINCLUDE
        #include "UnityCG.cginc"
        struct appdata
        {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            fixed4 color : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct v2f
        {
            float4 vertex : SV_POSITION;
            fixed4 color : COLOR;
            UNITY_VERTEX_OUTPUT_STEREO
        };
        fixed4 _Color;
        float _SideShade;
        v2f vert(appdata v)
        {
            v2f o;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_OUTPUT(v2f, o);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.color = v.color * _Color;
            o.color.rgb *= lerp(1 - _SideShade, 1, abs(v.normal.z));
            return o;
        }
        fixed4 frag(v2f i) : SV_Target { return i.color; }
        ENDCG
        Pass
        {
            Tags { "LightMode"="Always" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            ENDCG
        }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            ENDCG
        }
    }
    Fallback Off
}
