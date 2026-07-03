using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;
using Artngame.PDM;

namespace Artngame.PDM {
	
	[ExecuteInEditMode()]
	[System.Serializable()]
	public class SKinColoredParticles : MonoBehaviour {
		
		void OnEnable(){
			if(!Application.isPlaying){
				
				p11 = this.gameObject.GetComponent<ParticleSystem>();
			}
		}
		
		Vector3[] vertices ;
		public float Scale_factor=1f;
		Color32[] colorsA  ;
		
		public float Start_size=0.2f;
		
		Color[] Pcolors;
		
		
		ParticleSystem.Particle[] ParticleList;
		
		
		
		public GameObject emitter;
		
		[SerializeField,HideInInspector]
		public SkinnedMeshRenderer mesh ;
		
		[SerializeField,HideInInspector]
		public MeshFilter simple_mesh ;
		
		[SerializeField,HideInInspector]
		public Mesh animated_mesh;
		
		
		public bool Colored=true;

		
		
		public bool face_emit=false;
		
		void Start () {
			
			if(simple_mesh!=null & 1==1){
				
				
				if(1==1){
					if(Application.isPlaying){
						vertices = simple_mesh.mesh.vertices;
					}
					else{
						vertices = simple_mesh.sharedMesh.vertices;
					}
					
					
					
					
					if(!face_emit){

						if(p11.main.maxParticles!=(int)(vertices.Length/Every_other_vertex)){
							ParticleSystem.MainModule main = p11.main;//v2.3
							main.maxParticles=(int)(vertices.Length/Every_other_vertex);
						}

						
						p11.Emit((int)(vertices.Length/Every_other_vertex));
					}
					if(face_emit){
						
						if(!let_loose){
							p11.Emit(p11.main.maxParticles);
						}
						
					}
				}
				
			}
			
			if(mesh!=null){
				
				if(animated_mesh!=null){
					mesh.BakeMesh(animated_mesh);
					
					
					vertices = animated_mesh.vertices;
					{
						
						
						if(!face_emit){

							if(p11.main.maxParticles!=(int)(vertices.Length/Every_other_vertex)){
								ParticleSystem.MainModule main = p11.main;//v2.3
								main.maxParticles=(int)(vertices.Length/Every_other_vertex);
							}

							p11.Emit((int)(vertices.Length/Every_other_vertex)); 
						}
						if(face_emit){
							
							p11.Emit(p11.main.maxParticles); 
						}
						Debug.Log (p11.particleCount);
						
						
					}
				}
			}
			
			
			Particle_Num = particle_count;
			
			Registered_paint_positions = new List<Vector3>();
			Registered_paint_rotations = new List<Vector3>();
			
			noise = new PerlinPDM ();
			

			
			if(Application.isPlaying){
				
			
				

				
				
				if(face_emit){
					if(!extend_life){
						//p11.Clear ();
					}
				}
				
				if(!face_emit){
					
					p11.Clear ();
					
				}
				
			}
			
			if(p11==null ){
				p11 = this.gameObject.GetComponent<ParticleSystem>();
			}
			
			if(p11 !=null){
				
				keep_max_particle_number = p11.main.maxParticles;
				
			}else{
				
				Debug.Log ("Please attach a gameobject with skinned mesh or meshfilter as emitter and attach script to a particle system");
			}
			
		}
		
		private int keep_max_particle_number;
		

		//[Range(0.1f,100)]
		public float Every_other_vertex=1;
		
		public int particle_count = 100;
		
		bool got_positions=false;
		
		
		Vector3[] positions;
		Vector2[] rand_offsets;
		int[] tile;
		
		public ParticleSystem p11;
		
		
		
		private List<Vector3> Registered_paint_positions; 
		private List<Vector3> Registered_paint_rotations; 
		
			

		
		public float Y_offset=0f;
		
		public bool fix_initial = false;
		private bool let_loose = false;
		public bool Let_loose = false;

		public float keep_in_position_factor =0.95f;
		public float keep_alive_factor =0.5f;

		private int place_start_pos;
		
		public bool extend_life=false;
		
		public bool Gravity_Mode=false;

		[Header("Inward / Absorb Motion")]
		public bool Inward_Mode = false;
		public float Outer_scale = 0.05f;
		public float Inner_scale = 0.01f;
		public float Inward_speed = 0.03f;
		public float Outer_jitter = 0.008f;
		public float Outer_random_scale = 0.25f;
		public Transform Inward_Target;
		

		

		
		private int Particle_Num;
		


		private PerlinPDM  noise;

		
		public float Return_speed=0.005f;

		float Stable01(int seed)
		{
			float v = Mathf.Sin(seed * 12.9898f + 78.233f) * 43758.5453f;
			return v - Mathf.Floor(v);
		}

		Vector3 StableUnitVector(int seed)
		{
			float x = Stable01(seed * 3 + 1) * 2f - 1f;
			float y = Stable01(seed * 3 + 2) * 2f - 1f;
			float z = Stable01(seed * 3 + 3) * 2f - 1f;

			Vector3 v = new Vector3(x, y, z);
			if (v.sqrMagnitude < 0.0001f)
			{
				v = Vector3.up;
			}

			return v.normalized;
		}

		Texture2D GetMaterialTexture(Renderer rend)
		{
			if (rend == null) { return null; }

			Material mat = Application.isPlaying ? rend.material : rend.sharedMaterial;
			if (mat == null) { return null; }

			if (mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") != null)
			{
				return mat.GetTexture("_MainTex") as Texture2D;
			}

			if (mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") != null)
			{
				return mat.GetTexture("_BaseMap") as Texture2D;
			}

			return mat.mainTexture as Texture2D;
		}

		Vector2 GetMaterialTextureOffset(Renderer rend)
		{
			if (rend == null) { return Vector2.zero; }

			Material mat = Application.isPlaying ? rend.material : rend.sharedMaterial;
			if (mat == null) { return Vector2.zero; }

			if (mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") != null)
			{
				return mat.GetTextureOffset("_MainTex");
			}

			if (mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") != null)
			{
				return mat.GetTextureOffset("_BaseMap");
			}

			return mat.mainTextureOffset;
		}

		Vector3 GetOuterPosition(Vector3 vertexLocal, int particleIndex)
		{
			Vector3 dir = vertexLocal.normalized;

			if (dir == Vector3.zero)
			{
				dir = StableUnitVector(particleIndex + 17);
			}

			// particleIndex 기반 고정 랜덤.
			// Random.InitState를 쓰지 않아서 매 프레임 타겟이 흔들리지 않는다.
			float randomScale = 1f + ((Stable01(particleIndex * 11 + 5) * 2f - 1f) * Outer_random_scale);
			Vector3 jitterLocal = StableUnitVector(particleIndex * 13 + 9) * Stable01(particleIndex * 17 + 3) * Outer_jitter;

			Vector3 outerLocal = dir * vertexLocal.magnitude * Outer_scale * randomScale + jitterLocal;

			return emitter.transform.TransformPoint(outerLocal);
		}

		Vector3 GetInnerPosition(Vector3 vertexLocal)
		{
				Vector3 innerLocal = vertexLocal * Inner_scale;
				return emitter.transform.TransformPoint(innerLocal);
		}

		void ApplyInwardMotion(ref ParticleSystem.Particle particle, Vector3 vertexLocal, int index)
		{
				Vector3 outerPos = GetOuterPosition(vertexLocal, index);
				Vector3 innerPos = GetInnerPosition(vertexLocal);
				Vector3 finalTarget = Inward_Target != null ? Inward_Target.position : innerPos;

				// remainingLifetime 기준으로 초반에는 무조건 바깥 위치에 둔다.
				// 이게 있어야 Outer_scale을 키웠을 때 진짜 바깥에 있는 파티클이 보인다.
				bool stayOuter = particle.remainingLifetime > particle.startLifetime * keep_in_position_factor;

				if (stayOuter)
				{
						particle.position = outerPos;
				}
				else
				{
						particle.position = Vector3.Lerp(
								particle.position,
								finalTarget,
								Mathf.Clamp01(Inward_speed)
						);
				}

				particle.velocity = Vector3.Lerp(
						particle.velocity,
						Vector3.zero,
						0.12f
				);
		}


		void Update () {
			
			
			
			if(emitter ==null | p11 == null){
				
				
				//Debug.Log ("Please attach a gameobject with skinned mesh or meshfilter as emitter and attach script to a particle system"); 
				
				return;
			}

			if(Every_other_vertex <0.05f){
				Every_other_vertex = 0.05f;
			}

			
			if(mesh ==null & Application.isPlaying){
				mesh = emitter.GetComponent<SkinnedMeshRenderer>();
			}

			//v1.2.2
			if(animated_mesh==null & !Application.isPlaying){
				animated_mesh = new Mesh();
				animated_mesh.hideFlags = HideFlags.HideAndDontSave;
			}

			if(animated_mesh==null & Application.isPlaying){
				
				animated_mesh = new Mesh();
				
				if(mesh!=null){
					mesh.BakeMesh(animated_mesh);
				}
				
			}
			
			if(simple_mesh==null & Application.isPlaying){
				simple_mesh = emitter.GetComponent<MeshFilter>();
			}
			
			
			////////////////// SKINNED //////////////
			
			if(p11 != null){ 
				if(p11.main.startSize.constant < Start_size & Application.isPlaying){//if(p11.main.startSize < Start_size & Application.isPlaying){ //v2.3
					ParticleSystem.MainModule main = p11.main;//v2.3
					ParticleSystem.MinMaxCurve Curve = main.startSize;
					Curve.constant = main.startSize.constant +(Start_size/3); //v2.3
					//main.startSize = main.startSize+(Start_size/3);
				}else{
					
				}
				
				//reset if changed parricle max number
				if(p11.main.maxParticles != keep_max_particle_number){
					Start ();
					got_positions=false;
					positions=null;
					keep_max_particle_number=p11.main.maxParticles;
					Debug.Log ("adjusted");
				}
				
			}
			
			if(Every_other_vertex<=0){
				Every_other_vertex=1;
			}
			
			if(mesh!=null){
				if(animated_mesh!=null){
					mesh.BakeMesh(animated_mesh);
				}
			}
			///////////////// END SKINNED //////////////
			if(1==1){

				
				if(Particle_Num != particle_count){
					
					got_positions=false;
					Particle_Num = particle_count;
				}
				
				let_loose = Let_loose;
				if(!Application.isPlaying){ 
					
					//let_loose = false;
					
					if(noise ==null){
						noise = new PerlinPDM ();
					}
					
				}
				
				int tileCount=15;
				
				#region A
				
				if(simple_mesh!=null & 1==1){
					
					if(Application.isPlaying){
						vertices = simple_mesh.mesh.vertices;
					}
					else{
						vertices = simple_mesh.sharedMesh.vertices;
					}
					
					
					if(!face_emit){

						if(p11.main.maxParticles!=(int)(vertices.Length/Every_other_vertex)){
							ParticleSystem.MainModule main = p11.main;//v2.3
							main.maxParticles=(int)(vertices.Length/Every_other_vertex);
						}

						
						p11.Emit((int)(vertices.Length/Every_other_vertex));
					}
					if(face_emit){
						
						
						if(p11.particleCount<p11.main.maxParticles){
							
							p11.Emit(p11.main.maxParticles);
						}
					}
					
				}
				
				if(mesh!=null){
					
					if(animated_mesh!=null){
						mesh.BakeMesh(animated_mesh);
						vertices = animated_mesh.vertices;
						{
							
							if(!face_emit){

								if(p11.main.maxParticles!=(int)(vertices.Length/Every_other_vertex)){
									ParticleSystem.MainModule main = p11.main;//v2.3
									main.maxParticles=(int)(vertices.Length/Every_other_vertex);
								}

								p11.Emit((int)(vertices.Length/Every_other_vertex)); 
							}
							if(face_emit){
								
								p11.Emit(p11.main.maxParticles); 
							}
							
						}
					}
				}
				
				if(vertices!=null){
					int DIVIDED_VERTEXES = (int)(vertices.Length/Every_other_vertex);
					
					if(face_emit){
						DIVIDED_VERTEXES = p11.main.maxParticles;
					}
					
					
					
					if(  p11.particleCount >= (DIVIDED_VERTEXES)){ 
						
						if(simple_mesh!=null){
							
							if(Application.isPlaying){
								vertices = simple_mesh.mesh.vertices;
							}else{vertices = simple_mesh.sharedMesh.vertices;}






							int ParticlesNeeded = vertices.Length;
							
							if(p11.particleCount < ParticlesNeeded){
							}
							
							int Count_uvs =0;
							
							if(Application.isPlaying){
								
								Count_uvs = simple_mesh.mesh.uv2.Length;
							}else{
								
								Count_uvs = simple_mesh.sharedMesh.uv2.Length;
							}
							
							Vector2[] uvs    = new Vector2[Count_uvs];
							
							if(Application.isPlaying){
								
								uvs = simple_mesh.mesh.uv2;
							}else{
								
								uvs = simple_mesh.sharedMesh.uv2; 
							}
							
							colorsA = new Color32[ uvs.Length ]; 
							Vector2 offset1 = Vector2.zero;
							
							if(Application.isPlaying){
								offset1 = simple_mesh.gameObject.GetComponent<Renderer>().material.mainTextureOffset;
							}
							else{
								offset1 = simple_mesh.gameObject.GetComponent<Renderer>().sharedMaterial.mainTextureOffset;
							}
							
							Texture2D pixels =  simple_mesh.gameObject.GetComponent<Renderer>().sharedMaterial.mainTexture as Texture2D;
							
							int uvl = uvs.Length;
							
							if(this.transform.parent != null){

								if(this.transform.parent.GetComponent<Renderer>().sharedMaterial.mainTexture!=null){
									if(this.transform.parent.GetComponent<Renderer>().sharedMaterial.mainTexture.filterMode == FilterMode.Bilinear){
										for ( int j=0; j<uvl; j++) {
											
											Vector2 uv = uvs[ j ];
											
											colorsA[ j ] = pixels.GetPixelBilinear( ( uv.x)+offset1.x , ( uv.y)+offset1.y  );
											
										}
									}else{colorsA = pixels.GetPixels32();}
								}

							}else{
								Debug.Log ("Please attach the particle to the emitter mesh object");
							}



							if(p11 != null){ 
								
								
								ParticleList = new ParticleSystem.Particle[p11.particleCount];
								p11.GetParticles(ParticleList);
								
								if(p11.particleCount >=(DIVIDED_VERTEXES)){
									
									int count_vertices=0;
									for (int i=0; i < ParticleList.Length;i++)
									{
										
										
										if(extend_life){
											if(tile!=null){
												if(i<tile.Length){
													ParticleList[i].remainingLifetime = tileCount + 1 - tile[i];
												}
											}
										}

										if(ParticleList[i].remainingLifetime < ParticleList[i].startLifetime*keep_alive_factor){
											ParticleList[i].startLifetime = tileCount;
										}
										
										if(!let_loose){
											
											if(!face_emit){
												ParticleList[i].position = vertices[count_vertices]*Scale_factor + new Vector3(0f,0f,0f)+ p11.transform.position;
											}else{
												
												if(positions!=null ){ 
													if(positions!=null & i<positions.Length){
														ParticleList[i].position = positions[i];
													}}
											}
											
										}
										if(let_loose){
											
											if(positions!=null){ 
												if(positions!=null & i<positions.Length){
													
													if(!Inward_Mode && !extend_life & ParticleList[i].remainingLifetime > ParticleList[i].startLifetime*keep_in_position_factor){
														if(!face_emit){
															ParticleList[i].position = vertices[count_vertices]*Scale_factor + new Vector3(0f,0f,0f)+ p11.transform.position;
														}else{
															
															if(positions!=null ){ 
																if(positions!=null & i<positions.Length){
																	ParticleList[i].position = positions[i];
																}}
														}
													}
													
													
													
													
													//Gravity
													if(let_loose & Gravity_Mode){
														if(Inward_Mode){
															ApplyInwardMotion(ref ParticleList[i], vertices[count_vertices], i);
														}else{
															ParticleList[i].position = Vector3.Slerp(ParticleList[i].position, positions[i]+ new Vector3(i*0.005f,Y_offset,i*0.007f),Return_speed);
															ParticleList[i].velocity= Vector3.Slerp(ParticleList[i].velocity,Vector3.zero,0.05f);
														}
													}
												}}
											
										}


										if(Colored){
											ParticleList[i].startColor = colorsA[count_vertices];
										}


										
										count_vertices=count_vertices+1;
										
										if(count_vertices>vertices.Length-1){
											count_vertices=0;
										}
									}
									
									p11.SetParticles(ParticleList,p11.particleCount);
								}
							}
							
						}
						
						if(mesh!=null & 1==1){


							Vector2[] uvs    =  animated_mesh.uv2;
							colorsA = new Color32[ uvs.Length ]; 
							
							Renderer skinnedRenderer = mesh.gameObject.GetComponent<Renderer>();
							Texture2D pixels = GetMaterialTexture(skinnedRenderer);
							
							int uvl = uvs.Length;
							for ( int j=0; j<uvl; j++) {
								
								Vector2 uv = uvs[ j ];
								
								colorsA[ j ] = pixels.GetPixelBilinear( ( uv.x) , ( uv.y)  );
							}


							if(p11 != null){ 
								
								if(p11.particleCount >=(DIVIDED_VERTEXES)){
									ParticleList = new ParticleSystem.Particle[p11.particleCount];
									p11.GetParticles(ParticleList);
									
									int count_vertices=0;
									
									for (int i=0; i < ParticleList.Length;i=i+1)
									{
										
										if(extend_life){
											if(tile!=null){
												if(i<tile.Length){
													ParticleList[i].remainingLifetime = tileCount + 1 - tile[i];
												}
											}
										}

										if(ParticleList[i].remainingLifetime < ParticleList[i].startLifetime*keep_alive_factor){
											ParticleList[i].startLifetime = tileCount;
										}
										
										if(!let_loose){
											
											ParticleList[i].position = emitter.transform.rotation*(vertices[count_vertices]*Scale_factor+new Vector3(0f,0f,0f))+ p11.transform.position;
											
										}
										
										if(let_loose){
											if(!Inward_Mode && !extend_life & ParticleList[i].remainingLifetime > (ParticleList[i].startLifetime*keep_in_position_factor)){
												
												ParticleList[i].position = emitter.transform.rotation*(vertices[count_vertices]*Scale_factor+new Vector3(0f,0f,0f))+ p11.transform.position;
												
											}
											
											//Gravity
											if(let_loose & Gravity_Mode){
												if(Inward_Mode){
													ApplyInwardMotion(ref ParticleList[i], vertices[count_vertices], i);
												}else{
													ParticleList[i].position = Vector3.Slerp(ParticleList[i].position, (emitter.transform.rotation*(vertices[count_vertices]*Scale_factor+new Vector3(0f,0f,0f))+ p11.transform.position),Return_speed);
													ParticleList[i].velocity= Vector3.Slerp(ParticleList[i].velocity,Vector3.zero,0.05f);
												}
											}
											
										}

										if(Colored){
											ParticleList[i].startColor = colorsA[count_vertices];
										}

										count_vertices=count_vertices+1;
										
										if(count_vertices>vertices.Length-1){
											count_vertices=0;
										}
										
									}
									p11.SetParticles(ParticleList,p11.particleCount);
								}
							}
							
						}
						
					}}
				
				#endregion
				if(ParticleList	!=null){	
					
					if(!got_positions | 1==0){
						positions = new Vector3[p11.particleCount];
						tile = new int[p11.particleCount];
						got_positions = true;
						
						for(int i=0;i<ParticleList.Length;i++){
							
							positions[i] = ParticleList[i].position;
							tile[i] = Random.Range(0,15);
						}
						
					}
					
					// PROJECTION
					
					
					
					
					
					
					
					if(!fix_initial){
						Registered_paint_positions.Clear();
						Registered_paint_rotations.Clear();
					}
					
					if(Registered_paint_positions!=null){
						
						for(int i=0;i<ParticleList.Length;i++){
							
							Registered_paint_positions.Add(ParticleList[i].position);
							Registered_paint_rotations.Add(Vector3.zero);
						}
						
					}
					

					
					
				}
				
			}
		}//end update
	}
	
}
