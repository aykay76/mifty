using System;
using System.Collections.Generic;

namespace mifty
{
    public class BTree<T>
    {
        private int order;
        private BTree<T> parent;
        private List<T> keys;
        private List<BTree<T>> children;

        public BTree(int order = 2)
        {
            this.order = order;
            this.keys = new List<T>();
            this.children = new List<BTree<T>>();
            this.parent = null;
        }

        // Search for a key in the tree. Either returns null or the node where the key is.
        public bool Find(IComparable<T> key)
        {
            // find the leaf node where it could be, then check the keys in that node
            BTree<T> leaf = FindLeaf(key);

            for (int i = 0; i < leaf.keys.Count; i++)
            {
                int comparison = key.CompareTo(keys[i]);

                // because the keys are ordered if the search key is less there is no point continuing
                if (comparison < 0)
                {
                    return false;
                }
                else if (comparison == 0)
                {
                    return true;
                }
            }

            // we have exhausted all options - search key was not found
            return false;
        }

        private BTree<T> FindLeaf(IComparable<T> key)
        {
            // step through the keys I have to see where the search key ranks
            for (int i = 0; i < keys.Count; i++)
            {
                // compare the search key against current key
                int comparison = key.CompareTo(keys[i]);
                
                // if less, check for left-hand child and recurse
                if (comparison < 0)
                {
                    // if no child exists then we have no further to go to find a match
                    if (children[i] == null)
                    {
                        return this;
                    }
                    else
                    {
                        // step down the tree
                        return children[i].FindLeaf(key);
                    }
                }
                else if (comparison == 0)
                {
                    // we have a match - return self
                    return this;
                }
            }

            // if we get this far we're on the right-hand side
            if (children.Count > keys.Count)
            {
                return children[order - 1].FindLeaf(key);
            }

            return this;
        }

        BTree<T> Insert(IComparable<T> key)
        {
            // to insert, find where the key could be inserted then check to see if there is room
            // if not we need to split and check parent etc.
            BTree<T> leaf = FindLeaf(key);

            return InsertSeparator(leaf, key);
        }

        BTree<T> InsertSeparator(BTree<T> leaf, IComparable<T> key)
        {
            // find the first key greater than the key to insert
            for (int i = 0; i < leaf.keys.Count; i++)
            {
                int comparison = key.CompareTo(leaf.keys[i]);
                if (comparison < 0)
                {
                    leaf.keys.Insert(i, (T)key);
                }
            }

            // if we have broken our limit we need to split and recurse upwards until we find a node that can settle or we create a new root
            if (leaf.keys.Count >= leaf.order - 1)
            {
                // otherwise we need to split
                BTree<T> newLeft = new BTree<T>(this.order);
                BTree<T> newRight = new BTree<T>(this.order);

                int s = leaf.keys.Count / 2;
                IComparable<T> separator = (IComparable<T>)leaf.keys[s];

                // everything below the separator gets added to the left
                for (int i = 0; i < s; i++)
                {
                    newLeft.keys.Add(leaf.keys[i]);
                }

                // everything above the separator gets added to the right
                for (int i = s + 1; i < leaf.keys.Count; i++)
                {
                    newRight.keys.Add(leaf.keys[i]);
                }

                // separator gets inserted into parent, or is a new root
                if (leaf.parent == null)
                {
                    BTree<T> newRoot = new BTree<T>(this.order);
                    newLeft.parent = newRoot;
                    newRight.parent = newRoot;
                    newRoot.keys.Add((T)separator);
                    newRoot.children.Add(newLeft);
                    newRoot.children.Add(newRight);
                    return newRoot;
                }
                else
                {
                    return InsertSeparator(leaf.parent, separator);
                }
            }

            return null;
        }

        BTree<T> Delete(T key)
        {
            return null;
        }
    }
}