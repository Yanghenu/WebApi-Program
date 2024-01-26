using System.Reflection;

namespace FusionProgram.Helpers
{
    public class CompareList
    {
        /// 对比两个同类型的List<T>返回差异List<T>集合
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="newModel">修改后的数据集合</param>
        /// <param name="oldModel">原始数据集合</param>
        /// <param name="keyField">数据主键</param>
        /// <returns>返回与原始集合有差异的集合</returns>
        public static Determine<T> GetModiflyList<T>(List<T> newModel, List<T> oldModel, string keyField)
        {
            int conint = 0;
            List<T> list = new List<T>();
            List<T> list2 = new List<T>();
            Determine<T> determine = new Determine<T>();
            foreach (T newMod in newModel)
            {
                conint++;

                //取得新实体的数据主键值
                object nob = newMod.GetType().GetProperty(keyField).GetValue(newMod, null);


                //根据主建找到老实体集合中主键值相等的实体
                T oldMol = oldModel.Find((delegate (T old)
                {
                    object ob = old.GetType().GetProperty(keyField).GetValue(old, null);

                    if (object.Equals(ob, nob))
                        return true;
                    else
                        return false;
                }));

                if (oldMol == null)
                {
                    list.Add(newMod);
                    continue;
                }

                PropertyInfo[] pi = oldMol.GetType().GetProperties();

                //将老实体对象的没一个属性值和新实体对象进行循环比较
                foreach (PropertyInfo p in pi)
                {
                    conint++;

                    object o_new = p.GetValue(newMod, null);
                    object o_old = p.GetValue(oldMol, null);

                    //新老实体比较并记录有差异的实体
                    if (object.Equals(o_new, o_old))
                        continue;
                    else
                    {
                        list2.Add(newMod);
                        break;
                    }
                }

            }
            determine.insertList = list;
            determine.updateList = list2;
            return determine;
        }

        public class Determine<T>
        {
            public List<T> updateList { get; set; }
            public List<T> insertList { get; set; }
        }
    }
}
