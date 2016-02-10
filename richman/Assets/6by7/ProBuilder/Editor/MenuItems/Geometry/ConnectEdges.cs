using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using ProBuilder2.MeshOperations;
using ProBuilder2.Common;
using ProBuilder2.EditorEnum;

namespace ProBuilder2.Actions
{
	public class SplitOperations : Editor
	{
		[MenuItem("Tools/ProBuilder/Geometry/Connect _&e", false,  pb_Constant.MENU_GEOMETRY + pb_Constant.MENU_GEOMETRY_USEINFERRED)]
		public static void MenuConnectInferUse()
		{
			switch(pb_Editor.instance.selectionMode)
			{
				case SelectMode.Vertex:
					MenuConnectVertices();
					break;

				case SelectMode.Face:
					MenuSubdivideFace();
					break;

				case SelectMode.Edge:
					MenuConnectEdges();
					break;
			}
		}
		
		[MenuItem("Tools/ProBuilder/Geometry/Subdivide Object")]
		public static void MenuSubdivideObject()
		{
			int c = 0;
			foreach(pb_Object pb in pbUtil.GetComponents<pb_Object>(Selection.transforms))
			{
				pbMeshOps.Subdivide(pb);
				pb.GenerateUV2(true);
				pb.Refresh();
				c++;
			}

	        pb_Editor_Utility.ShowNotification("Subdivide " + c + (c > 1 ? " objects" : " object"));
			pb_Editor.instance.UpdateSelection();			
		}

		[MenuItem("Tools/ProBuilder/Geometry/Subdivide Face (Alt+E)", false,  pb_Constant.MENU_GEOMETRY + pb_Constant.MENU_GEOMETRY_FACE + 2)]
		public static void MenuSubdivideFace()
		{
			pb_Object[] pbs = pbUtil.GetComponents<pb_Object>(Selection.transforms);
			int success = 0;

			foreach(pb_Object pb in pbs)
			{
				pbUndo.RecordObject(pb, "Connect Edges");
				
				List<EdgeConnection> split = new List<EdgeConnection>();
				foreach(pb_Face face in pb.SelectedFaces)
					split.Add(new EdgeConnection(face, new List<pb_Edge>(face.edges)));

				pb_Face[] faces;
				if(pb.ConnectEdges(split, out faces))
				{
					success++;
					pb.SetSelectedFaces(faces);
					pb.GenerateUV2(true);
					pb.Refresh();
				}
			}

			if(success > 0)
			{
		        pb_Editor_Utility.ShowNotification("Subdivide " + success + ((success > 1) ? " faces" : " face"));
				pb_Editor.instance.UpdateSelection();
			}
			else
			{
				Debug.LogWarning("Subdivide faces failed - did you not have any faces selected?");
			}
		}

		public static void MenuConnectEdges()
		{
			pb_Object[] pbs = pbUtil.GetComponents<pb_Object>(Selection.transforms);
			int success = 0;
			foreach(pb_Object pb in pbs)
			{
				pbUndo.RecordObject(pb, "Connect Edges");

				/// remove duplicate edges
				int len = pb.SelectedEdges.Length;
				List<EdgeConnection> splits = new List<EdgeConnection>();

				for(int i = 0; i < len; i++)
					foreach(pb_Face face in pbMeshUtils.GetConnectedFaces(pb, pb.SelectedEdges[i]))
						if(!splits.Contains((EdgeConnection)face))
						{
							List<pb_Edge> faceEdges = new List<pb_Edge>();
							foreach(pb_Edge e in pb.SelectedEdges)
							{
								int localEdgeIndex = face.edges.IndexOf(e, pb.sharedIndices);
								if(localEdgeIndex > -1)
									faceEdges.Add(face.edges[localEdgeIndex]);
							}
		
							if(faceEdges.Count > 1)	
								splits.Add(new EdgeConnection(face, faceEdges));
						}

				// List<pb_Face> f = new List<pb_Face>();
				// for(int i = 0; i < splits.Count; i++)
				// 	f.Add(splits[i].face);

				pb_Face[] faces;
				if(pb.ConnectEdges(splits, out faces))
				{
					success++;
					pb.SetSelectedFaces(faces);
					pb.GenerateUV2(true);
					pb.Refresh();
				}
			}

			if(success > 0)
			{
				pb_Editor.instance.UpdateSelection();
		        pb_Editor_Utility.ShowNotification( pb_Editor.instance.selectionMode == SelectMode.Edge ? "Connect Edges" : "Subdivide");
		    }
		}

		// [MenuItem("Tools/ProBuilder/Geometry/Connect Vertices", false,  pb_Constant.MENU_GEOMETRY + pb_Constant.MENU_GEOMETRY_VERTEX + 2)]
		public static void MenuConnectVertices()
		{
			pb_Object[] pbs = pbUtil.GetComponents<pb_Object>(Selection.transforms);
			int success = 0;

			foreach(pb_Object pb in pbs)
			{
				pbUndo.RecordObject(pb, "Connect Vertices");

				int len = pb.SelectedTriangles.Length;

				List<VertexConnection> splits = new List<VertexConnection>();
				List<pb_Face>[] connectedFaces = new List<pb_Face>[len];

				// For each vertex, get all it's connected faces
				for(int i = 0; i < len; i++)
					connectedFaces[i] = pbMeshUtils.GetConnectedFaces(pb, pb.SelectedTriangles[i]);

				for(int i = 0; i < len; i++)
				{
					foreach(pb_Face face in connectedFaces[i])
					{
						int index = splits.IndexOf((VertexConnection)face);	// VertexConnection only compares face property
						if(index < 0)
							splits.Add( new VertexConnection(face, new List<int>(1) { pb.SelectedTriangles[i] } ) );
						else
							splits[index].indices.Add(pb.SelectedTriangles[i]);
					}
				}

				pb_Face[] f;
				if(pb.ConnectVertices(splits, out f))
				{
					success++;
					pb.SetSelectedFaces(f);
				}
			}
			
			foreach(pb_Object pb in pbs)
			{
				pb.GenerateUV2(true);
				pb.Refresh();
			}

			if(success > 0)
			{
				pb_Editor_Utility.ShowNotification("Connect Vertices", "");
				pb_Editor.instance.UpdateSelection();
			}
			else
			{
				Debug.LogWarning("No valid split paths found.  This is most likely because you are attempting to split vertices that do not belong to the same face.  This is not currently supported, sorry!");
			}
		}
	}
}