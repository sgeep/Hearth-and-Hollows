#if UNITY_EDITOR
using UnityEditor;

namespace Clavian.STM.Editor
{
	public static class Tools
	{
		public static string AssetPath
		{
			get
			{
				string searchValue = "Clavian/SuperTextMesh";
				string returnPath = "";
				string[] allPaths = AssetDatabase.GetAllAssetPaths();
				for (int i = 0; i < allPaths.Length; i++)
				{
					if (allPaths[i].Contains(searchValue))
					{
						// This is the path we want! Let's strip out everything after the searchValue
						returnPath = allPaths[i];
						returnPath = returnPath.Remove(returnPath.IndexOf(searchValue));
						returnPath += searchValue;
						break;
					}
				}
				return returnPath;
			}
		}
	}
}
#endif