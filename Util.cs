using UnityEngine;

namespace FastTravel
{
    public static class Util
    {
        /// <summary>
        /// Find a game object's child recursively by name.
        /// </summary>
        /// <param name="go">The game object to search recursively through</param>
        /// <param name="childName">The name of the child to be found.</param>
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
