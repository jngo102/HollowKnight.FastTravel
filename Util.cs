using UnityEngine;

namespace FastTravel
{
    public static class Util
    {
        public static GameObject Find(this GameObject go, string childName)
        {
            foreach (Transform child in go.transform)
            {
                if (child.name == childName)
                {
                    return child.gameObject;
                }
                else
                {
                    GameObject foundChild = child.gameObject.Find(childName);
                    if (foundChild)
                    {
                        return foundChild;
                    }
                }
            }

            return null;
        }
    }
}
