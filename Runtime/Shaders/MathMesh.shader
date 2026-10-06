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
        struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; fixed4 color : COLOR; };
        struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; };
        fixed4 _Color;
        float _SideShade;
        v2f vert(appdata v)
        {
            v2f o;
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
            ENDCG
        }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }
    }
    Fallback Off
}
