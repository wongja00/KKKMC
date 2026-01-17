Shader "Custom/GrassShader"
{
    Properties
    {
        _MainTex ("Grass Texture", 2D) = "white" {}
        _Color ("Grass Color", Color) = (0.5, 0.8, 0.3, 1)
        _WindStrength ("Wind Strength", Range(0, 2)) = 1
        _WindSpeed ("Wind Speed", Range(0, 5)) = 1
        _WindDirection ("Wind Direction", Vector) = (1, 0, 0, 0)
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setupInstancing
            
            #include "UnityCG.cginc"
            
            struct GrassData
            {
                float3 position;
                float3 normal;
                float height;
                float width;
                float rotation;
                float windOffset;
                float health;
            };
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                uint instanceID : SV_InstanceID;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float3 normal : TEXCOORD2;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _WindStrength;
            float _WindSpeed;
            float4 _WindDirection;
            float _WindTime;
            
            StructuredBuffer<GrassData> _GrassData;
            
            void setupInstancing()
            {
                // This is called for each instance
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                
                // Get grass data for this instance
                GrassData grass = _GrassData[v.instanceID];
                
                // Skip dead grass
                if (grass.health <= 0.0)
                {
                    o.vertex = float4(0, 0, 0, 0);
                    return o;
                }
                
                // Apply wind effect
                float windEffect = sin(_WindTime + grass.windOffset) * _WindStrength;
                float3 windVector = float3(_WindDirection.x, 0, _WindDirection.y) * windEffect * 0.1;
                
                // Transform vertex
                float3 worldPos = grass.position + windVector;
                
                // Apply grass properties
                float3 rotatedVertex = v.vertex;
                rotatedVertex.x *= grass.width;
                rotatedVertex.y *= grass.height;
                
                // Apply rotation
                float s = sin(grass.rotation * UNITY_PI / 180.0);
                float c = cos(grass.rotation * UNITY_PI / 180.0);
                float2x2 rotationMatrix = float2x2(c, -s, s, c);
                rotatedVertex.xz = mul(rotationMatrix, rotatedVertex.xz);
                
                // Align to terrain normal
                float3 up = float3(0, 1, 0);
                float3 right = normalize(cross(up, grass.normal));
                up = normalize(cross(grass.normal, right));
                
                float3x3 normalMatrix = float3x3(right, up, grass.normal);
                rotatedVertex = mul(normalMatrix, rotatedVertex);
                
                worldPos += rotatedVertex;
                
                o.vertex = UnityWorldToClipPos(worldPos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = worldPos;
                o.normal = UnityObjectToWorldNormal(v.normal);
                
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                
                // Add some variation based on position
                float variation = sin(i.worldPos.x * 10 + i.worldPos.z * 10) * 0.1 + 0.9;
                col.rgb *= variation;
                
                return col;
            }
            ENDCG
        }
    }
}
