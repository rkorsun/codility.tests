using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Challenges
{
    public interface ICloningService
    {
        T Clone<T>(T source);
    }
     
    public class CloningService : ICloningService
    {
        private class TypeCahce        
        {
            public Type Type;
            public bool IsValueType;
            public Func<object, Dictionary<object, object>, object> CopyMethod;
        }

        private static readonly Type TYPE_OBJECT = typeof(object);
        private static readonly Type STR_TYPE = typeof(string);
        private static readonly Type TYPE_BOOL = typeof(bool);
        private static readonly Type TYPE_INT = typeof(int);
        private static readonly Type TYPE_DICT_OBJ_OBJ = typeof(Dictionary<object, object>);
        private static readonly Type TYPE_ICOLLECTION = typeof(ICollection);
        private static readonly Type TYPE_ARRAY = typeof(Array);

        private static readonly MethodInfo memberwiseClone = TYPE_OBJECT.GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
        private static Dictionary<Type, TypeCahce> typeCache = new Dictionary<Type, TypeCahce>();

        private static readonly MethodInfo thisClone = typeof(CloningService).GetMethod("DeepClone", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo thisArrayClone = typeof(CloningService).GetMethod("CloneArray", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo thisCollectionClone = typeof(CloningService).GetMethod("CloneCollection", BindingFlags.NonPublic | BindingFlags.Static);


        public T Clone<T>(T source)
        {
            if (source == null) {
                return default(T);
            }
            return (T)CopyObject(source);
        }
         
        private static Dictionary<object, object> visited = new Dictionary<object, object>(new ComparerByReference());
        private static object CopyObject(object srcObject)
        {            
            //var visited = new Dictionary<object, object>(new ComparerByReference());
            visited.Clear();
            var result = DeepClone(srcObject, visited);            
            return result;
        }

        private static TypeCahce GetTypeInfos(Type srcType)
        {
            TypeCahce result;
            if (!typeCache.TryGetValue(srcType, out result))
            {                
                result = new TypeCahce() { Type = srcType, IsValueType = srcType.IsValueType };

                ParameterExpression inputExpr = Expression.Parameter(TYPE_OBJECT);
                var visitedParam = Expression.Parameter(TYPE_DICT_OBJ_OBJ);

                if (!srcType.IsValueType && !srcType.IsArray && srcType.GetConstructor(Type.EmptyTypes) == null)
                {
                    result.CopyMethod = Expression.Lambda<Func<object, Dictionary<object, object>, object>>(
                        Expression.Constant(null, srcType), inputExpr, visitedParam).Compile(); 
                    typeCache.Add(srcType, result);
                    return result;

                }

                var memberwiseCloneExpr = Expression.Call(inputExpr, memberwiseClone);
                var resultVar = Expression.Variable(srcType);
                var srcVar = Expression.Variable(srcType);

                var copyExpressions = new List<Expression>();
                var copyVariables = new List<ParameterExpression>() { resultVar, srcVar };

                copyExpressions.Add(Expression.Assign(srcVar, Expression.Convert(inputExpr, srcType)));
                copyExpressions.Add(Expression.Assign(resultVar, Expression.Convert(memberwiseCloneExpr, srcType)));
                //copyExpressions.Add(Expression.Assign(resultVar, Expression.Convert(Expression.New(srcType), srcType)));


                if (srcType.IsArray)
                {                    
                    var arrCopyExpr = Expression.Assign(resultVar,
                        Expression.Convert(Expression.Call(thisArrayClone, Expression.Convert(srcVar, TYPE_ARRAY), visitedParam), srcType));
                    copyExpressions.Add(arrCopyExpr);
                }
                else if (TYPE_ICOLLECTION.IsAssignableFrom(srcType) && srcType.IsGenericType)
                {
                    var listCopyExpr = Expression.Assign(resultVar,
                        Expression.Convert(Expression.Call(thisCollectionClone, Expression.Convert(srcVar, TYPE_ICOLLECTION), visitedParam), srcType));
                    copyExpressions.Add(listCopyExpr);

                }
                else
                {
                    copyExpressions.Add(Expression.Assign(Expression.Property(visitedParam,
                            TYPE_DICT_OBJ_OBJ.GetProperty("Item"), inputExpr), Expression.Convert(resultVar, TYPE_OBJECT)));
                    BuildPropertiesExpression(result, srcType, srcVar, resultVar, visitedParam, copyExpressions);
                }
                
                copyExpressions.Add(Expression.Convert(resultVar, TYPE_OBJECT));
                var copyExpr = Expression.Lambda<Func<object, Dictionary<object, object>, object>>(
                Expression.Block(copyVariables, copyExpressions), inputExpr, visitedParam).Compile();

                result.CopyMethod = copyExpr;

                typeCache.Add(srcType, result);
                return result;
            }

            return result;
        }

        private static void BuildPropertiesExpression(TypeCahce result, 
            Type srcType, 
            ParameterExpression srcVar,
            ParameterExpression resultVar,
            ParameterExpression visitedParam,
            List<Expression> copyExpressions
            )
        {
            foreach (var eachField in srcType.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var mode = GetAttrMode(eachField);
                if (mode != CloningMode.Shallow)
                {
                    var fldCopyTo = Expression.Field(resultVar, eachField);
                    if (mode == CloningMode.Deep)
                    {
                        if (!IsPrimitive(eachField.FieldType))
                        {
                            bool needtToCheck = NeedToCheckType(eachField.FieldType);
                            var fldCopyFrom = Expression.Field(srcVar, eachField);
                            var chekType = Expression.Constant(needtToCheck, TYPE_BOOL);
                            var fldCopyExpr = Expression.Assign(fldCopyTo,
                                    Expression.Convert(Expression.Call(thisClone, Expression.Convert(fldCopyFrom, TYPE_OBJECT), visitedParam, chekType), eachField.FieldType));
                            if (eachField.FieldType.IsValueType)
                            {
                                copyExpressions.Add(fldCopyExpr);
                            }
                            else
                            {
                                var copyIfNotNull = Expression.IfThen(Expression.NotEqual(fldCopyFrom, Expression.Constant(null, eachField.FieldType)), fldCopyExpr);
                                if (needtToCheck)
                                {                      
                                    // todo: add more type checks
                                    copyExpressions.Add(Expression.IfThen(Expression.Not(Expression.TypeEqual(fldCopyFrom, TYPE_INT)), copyIfNotNull));
                                }
                                else
                                {
                                    copyExpressions.Add(copyIfNotNull);
                                }
                            }

                        }
                    }
                    else //if (mode == CloningMode.Ignore)
                    {
                        copyExpressions.Add(Expression.Assign(fldCopyTo, Expression.Default(eachField.FieldType)));
                    }
                }
            }
            foreach (var eachProp in srcType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!eachProp.CanWrite)
                {
                    continue;
                }
                var mode = GetAttrMode(eachProp);
                if (mode != CloningMode.Shallow)
                {
                    var propCopyTo = Expression.Property(resultVar, eachProp);
                    if (mode == CloningMode.Deep)
                    {
                        if (!IsPrimitive(eachProp.PropertyType))
                        {
                            bool needtToCheck = NeedToCheckType(eachProp.PropertyType);
                            var chekType = Expression.Constant(needtToCheck, TYPE_BOOL);
                            var propCopyFrom = Expression.Property(srcVar, eachProp);
                            var propCopyExpr = Expression.Assign(propCopyTo, Expression.Convert(
                                Expression.Call(thisClone, Expression.Convert(propCopyFrom, TYPE_OBJECT), visitedParam, chekType),
                                eachProp.PropertyType));
                            if (eachProp.PropertyType.IsValueType)
                            {
                                copyExpressions.Add(propCopyExpr);
                            }
                            else
                            {
                                var copyIfNotNUllExpr = Expression.IfThen(Expression.NotEqual(propCopyFrom, Expression.Constant(null, eachProp.PropertyType)), propCopyExpr);
                                if (needtToCheck)
                                {
                                    // todo: add more type checks
                                    copyExpressions.Add(Expression.IfThen(Expression.Not(Expression.TypeEqual(propCopyFrom, TYPE_INT)), copyIfNotNUllExpr));
                                }
                                else
                                {
                                    copyExpressions.Add(copyIfNotNUllExpr);
                                }
                                
                            }
                        }
                    }
                    else //if (mode == CloningMode.Ignore)
                    {
                        copyExpressions.Add(Expression.Assign(propCopyTo, Expression.Default(eachProp.PropertyType)));
                    }
                }
            }

        }

        private static bool NeedToCheckType(Type type)
        {
            return type == TYPE_OBJECT || TYPE_ICOLLECTION.IsAssignableFrom(type);
        }

        private static object CloneArray(Array srcObject, Dictionary<object, object> visited) 
        {            
            if (srcObject == null || srcObject.Rank != 1) {
                return null;
            }
            var elemType = srcObject.GetType().GetElementType();
            var result = srcObject.Clone() as Array;
            visited.Add(srcObject, result);           
            for (int i = 0; i < result.Length; i++)
            {
                var eachVal = srcObject.GetValue(i);
                var eachItemType = eachVal.GetType();
                if (IsPrimitive(eachItemType))
                {
                    result.SetValue(eachVal, i);
                }
                else
                {
                    if (eachVal != null)
                    {
                        result.SetValue(DeepClone(eachVal, visited), i);
                    }
                }                
            }

            return result;
        }

        private static object CloneCollection(object srcObject,  Dictionary<object, object> visited)
        {
            var props = srcObject.GetType().GetProperties();
            var listItemType = (props[props.Length - 1]).PropertyType;
            var listType = typeof(List<>).MakeGenericType(listItemType);
            var newList = (IList)Activator.CreateInstance(listType);
            var newCollection = newList as ICollection;
            visited.Add(srcObject, newCollection);
            foreach (var eachItem in (srcObject as ICollection))
            {
                var eachItemType = eachItem.GetType();                
                if (IsPrimitive(eachItemType))
                {
                    newList.Add(eachItem);
                }
                else
                {
                     var newVal = eachItem != null ? DeepClone(eachItem, visited) : null;
                     newList.Add(newVal);
                }

            }
            return newCollection;

        }

        private static TypeCahce lastTypeUsed;
        private static object DeepClone(object srcObject, Dictionary<object,object> visited, bool checkType = true)
        {
            object existing;
            if (visited.TryGetValue(srcObject, out existing))
            { 
                return existing; 
            }
            var srcType = srcObject.GetType();
            // FOR DEEP TREES OR COLLECTIONS OF SAME TYPE
            if (null == lastTypeUsed || lastTypeUsed.Type != srcType)
            {
                lastTypeUsed = GetTypeInfos(srcType); 
            }
            var typeCache = lastTypeUsed ?? GetTypeInfos(srcObject.GetType()); 
            return typeCache.CopyMethod(srcObject, visited); 
        }


        private static CloningMode GetAttrMode(PropertyInfo props)
        {
            if (props == null)
            {
                return CloningMode.Ignore;
            }

            return GetCloningMode(props.GetCustomAttributes(typeof(CloneableAttribute), false));
        }

        private static CloningMode GetAttrMode(FieldInfo props)
        {
            if (props == null)
            {
                return CloningMode.Ignore;
            }
            return GetCloningMode(props.GetCustomAttributes(typeof(CloneableAttribute), false));
        }

        private static CloningMode GetCloningMode(object[] attrArray)
        {            
            if (attrArray == null || attrArray.Length == 0)
            {
                return CloningMode.Deep;
            }

            var attr = attrArray[0] as CloneableAttribute;
            return attr?.Mode ?? CloningMode.Deep;
        }

        private static bool IsPrimitive(Type type)
        {
            return type == STR_TYPE || type.IsPrimitive;
        }

        private class ComparerByReference : EqualityComparer<object>
        {
            public override bool Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }
            public override int GetHashCode(object obj)
            {
                if (obj == null) return 0;
                return obj.GetHashCode();
            }
        }

    }

    public enum CloningMode
    {
        Deep = 0,
        Shallow = 1,
        Ignore = 2,
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class CloneableAttribute : Attribute
    {
        public CloningMode Mode { get; }

        public CloneableAttribute(CloningMode mode)
        {
            Mode = mode;
        }
    }

    public class CloningServiceTest
    {
        public class Simple
        {
            public int I;
            public string S { get; set; }
            [Cloneable(CloningMode.Ignore)]
            public string Ignored { get; set; }
            [Cloneable(CloningMode.Shallow)]
            public object Shallow { get; set; }

            public virtual string Computed => S + I + Shallow;
        }

        public struct SimpleStruct
        {
            public int I;
            public string S { get; set; }
            [Cloneable(CloningMode.Ignore)]
            public string Ignored { get; set; }

            public string Computed => S + I;

            public SimpleStruct(int i, string s)
            {
                I = i;
                S = s;
                Ignored = null;
            }
        }

        public class Simple2 : Simple
        {
            public double D;
            public SimpleStruct SS;
            public override string Computed => S + I + D + SS.Computed;
        }

        public class Node
        {
            public Node Left;
            public Node Right;
            public object Value;
            public int TotalNodeCount =>
                1 + (Left?.TotalNodeCount ?? 0) + (Right?.TotalNodeCount ?? 0);

            public Node Clone() {
                Node result = new Node();
                if (Left != null) result.Left = Left.Clone();
                if (Right != null) result.Left = Left.Clone();
                result.Value = Value;
                return result;
            }
        }

        public ICloningService Cloner = new CloningService();
        public Action[] AllTests => new Action[] {
            SimpleTest,
            SimpleStructTest,
            Simple2Test,
            NodeTest,
            ArrayTest,
            CollectionTest,
            ArrayTest2,
            CollectionTest2,
            MixedCollectionTest,
            RecursionTest,
            RecursionTest2,            
            PerformanceTest,
            NoConstructorTest,
            PerformanceTest2NewObj
        };

        public static void Assert(bool criteria)
        {
            if (!criteria)
                throw new InvalidOperationException("Assertion failed.");
        }

        public void Measure(string title, Action test)
        {
            test(); // Warmup
            var sw = new Stopwatch();
            GC.Collect();
            sw.Start();
            test();
            sw.Stop();
            Console.WriteLine($"{title}: {sw.Elapsed.TotalMilliseconds:0.000}ms");
        }

        public void SimpleTest()
        {
            var s = new Simple() { I = 1, S = "2", Ignored = "3", Shallow = new object() };
            var c = Cloner.Clone(s);
            Assert(s != c);
            Assert(s.Computed == c.Computed);
            Assert(c.Ignored == null);
            Assert(ReferenceEquals(s.Shallow, c.Shallow));
        }

        public void SimpleStructTest()
        {
            var s = new SimpleStruct(1, "2") { Ignored = "3" };
            var c = Cloner.Clone(s);
            Assert(s.Computed == c.Computed);
            Assert(c.Ignored == null);
        }

        public void Simple2Test()
        {
            var s = new Simple2()
            {
                I = 1,
                S = "2",
                D = 3,
                SS = new SimpleStruct(3, "4"),
            };
            var c = Cloner.Clone(s);
            Assert(s != c);
            Assert(s.Computed == c.Computed);
        }

        public void NodeTest()
        {
            var s = new Node
            {
                Left = new Node
                {
                    Right = new Node()
                },
                Right = new Node()
            };
            var c = Cloner.Clone(s);
            Assert(s != c);
            Assert(s.TotalNodeCount == c.TotalNodeCount);
        }

        public void RecursionTest()
        {
            var s = new Node();
            s.Left = s;
            var c = Cloner.Clone(s);
            Assert(s != c);
            Assert(null == c.Right);
            Assert(c == c.Left);
        }

        public void ArrayTest()
        {
            var n = new Node
            {
                Left = new Node
                {
                    Right = new Node()
                },
                Right = new Node()
            };
            var s = new[] { n, n };
            var c = Cloner.Clone(s);
            Assert(s != c);
            Assert(s.Sum(n1 => n1.TotalNodeCount) == c.Sum(n1 => n1.TotalNodeCount));
            Assert(c[0] == c[1]);
        }

        public void CollectionTest()
        {
            var n = new Node
            {
                Left = new Node
                {
                    Right = new Node()
                },
                Right = new Node()
            };
            var s = new List<Node>() { n, n };
            var c = Cloner.Clone(s);
            Assert(s != c);
            Assert(s.Sum(n1 => n1.TotalNodeCount) == c.Sum(n1 => n1.TotalNodeCount));
            Assert(c[0] == c[1]);
        }

        public void ArrayTest2()
        {
            var s = new[] { new[] { 1, 2, 3 }, new[] { 4, 5 } };
            var c = Cloner.Clone(s);
            Assert(s != c);
            Assert(15 == c.SelectMany(a => a).Sum());
        }

        public void CollectionTest2()
        {
            var s = new List<List<int>> { new List<int> { 1, 2, 3 }, new List<int> { 4, 5 } };
            var c = Cloner.Clone(s);
            Assert(s != c);
            Assert(15 == c.SelectMany(a => a).Sum());
        }

        public void MixedCollectionTest()
        {
            var s = new List<IEnumerable<int[]>> {
                new List<int[]> {new [] {1}},
                new List<int[]> {new [] {2, 3}},
            };
            var c = Cloner.Clone(s);
            Assert(s != c);
            Assert(6 == c.SelectMany(a => a.SelectMany(b => b)).Sum());
        }

        public void RecursionTest2()
        {
            var l = new List<Node>();
            var n = new Node { Value = l };
            n.Left = n;
            l.Add(n);
            var s = new object[] { null, l, n };
            s[0] = s;
            var c = Cloner.Clone(s);
            Assert(s != c);
            Assert(c[0] == c);
            var cl = (List<Node>)c[1];
            Assert(l != cl);
            var cn = cl[0];
            Assert(n != cn);
            Assert(cl == cn.Value);
            Assert(cn.Left == cn);
        }

        public void PerformanceTest()
        {
            Func<int, Node> makeTree = null;
            makeTree = depth => {
                if (depth == 0)
                    return null;
                return new Node
                {
                    Value = depth,
                    Left = makeTree(depth - 1),
                    Right = makeTree(depth - 1),
                };
            };
            for (var i = 10; i <= 20; i++)
            {
                var root = makeTree(i);
                Measure($"Cloning {root.TotalNodeCount} nodes", () => {
                    var copy = Cloner.Clone(root);
                    Assert(root != copy);
                });
            }
        }

        public void PerformanceTest2NewObj()
        {
            Measure($"Creating nodes with new()", () =>
            {
                //List<Node> list=new List<Node>()
                for (var i = 0; i <= 2000000; i++)
                {
                    var node = new Node();    
                }                               
            });
            Measure($"Creating nodes with expression()", () =>
            {                
                var newExpr = Expression.Lambda<Func<object>>(Expression.New(typeof(Node))).Compile();
                for (var i = 0; i <= 2000000; i++)
                {
                    var node = newExpr();
                }
            });
            var clonMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
            Measure($"Creating nodes with expression memberwiseclone()", () =>
            {
                var protNode = new Node();
                var protExpr = Expression.Parameter(typeof(object));
                var newExpr = Expression.Lambda<Func<object,object>>(Expression.Call(protExpr, clonMethod), protExpr).Compile();
                for (var i = 0; i <= 2000000; i++)
                {
                    var node = newExpr(protNode);
                }
            });
        }

        class HiddenConstructorClass
        {
            public object F { get; set; }
            public HiddenConstructorClass(object f)
            { 
            }
        }
        public void NoConstructorTest()
        {
            var h = new HiddenConstructorClass(2);
            var c = Cloner.Clone(h);
            Assert(null == c);
        }

        public void RunAllTests()
        {
            foreach (var test in AllTests)
                test.Invoke();
            Console.WriteLine("Done.");
        }
    }

    public class Solution
    {
        public static void Main(string[] args)
        {
            var cloningServiceTest = new CloningServiceTest();
            var allTests = cloningServiceTest.AllTests;
            while (true)
            {
                var line = Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                    break;
                var test = allTests[int.Parse(line)];
                try
                {
                    test.Invoke();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed on {test.GetMethodInfo().Name}. {ex.Message}");
                }
            }
            Console.WriteLine("Done.");
        }
    }
}
