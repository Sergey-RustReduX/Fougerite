using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace Fougerite
{
    public class JsonAPI
    {
        private static JsonAPI _inst;

        public string SerializeObjectToJson(object target)
        {
            return JsonConvert.SerializeObject(target);
        }

        public object DeSerializeJsonToObject(string target)
        {
            return JsonConvert.DeserializeObject(target);
        }

        public T DeSerializeJsonToObject<T>(string target)
        {
            return JsonConvert.DeserializeObject<T>(target);
        }

        public object SerializeXmlNode(System.Xml.XmlNode target)
        {
            return JsonConvert.SerializeXmlNode(target);
        }

        public object DeserializeXmlNode(string target)
        {
            return JsonConvert.DeserializeXmlNode(target);
        }

        public Type DeserializeAnonymousType<T>(string target, Type t)
        {
            return JsonConvert.DeserializeAnonymousType(target, t);
        }

        public JArray CreateJsonArray()
        {
            return new JArray();
        }

        public JObject CreateJsonObject()
        {
            return new JObject();
        }
        
        public JObject CreateJsonObject(object[] objects)
        {
            return new JObject(objects);
        }

        public JSchema CreateJSchema(string json)
        {
            return JSchema.Parse(json);
        }

        public JSchemaGenerator CreateJSchemaGenerator()
        {
            return new JSchemaGenerator();
        }

        public JSchema GenerateJSchema(Type specifiedclasstype)
        {
            JSchemaGenerator generator = new JSchemaGenerator();
            return generator.Generate(specifiedclasstype);
        }

        public JsonTextReader CreateJsonTextReader(string s)
        {
            return new JsonTextReader(new StringReader(s));
        }

        public JSchemaValidatingReader CreateJSchemaVReader(JsonReader reader)
        {
            return new JSchemaValidatingReader(reader);
        }

        public JsonSerializer CreateJsonSerializer()
        {
            return new JsonSerializer();
        }

        public JsonWriter CreateJsonWriter(StringBuilder sb, bool idented = false)
        {
            StringWriter sw = new StringWriter(sb);
            JsonTextWriter jtw = new JsonTextWriter(sw);
            if (idented) jtw.Formatting = Formatting.Indented;
            return jtw;
        }

        public StringBuilder CreateStringBD()
        {
            return new StringBuilder();
        }

        public JProperty CreateJProperty(string key, object value)
        {
            return new JProperty(key, value);
        }
        public JProperty CreateJProperty(string key, object[] value)
        {
            return new JProperty(key, value);
        }

        public JTokenReader CreateJTokenReader(JObject jo)
        {
            return new JTokenReader(jo);
        }

        public StringReader CreateStringReader(string json)
        {
            return new StringReader(json);
        }

        public static JsonAPI GetInstance
        {
            get { return _inst ?? (_inst = new JsonAPI()); }
        }

        public static void SaveFile<T>(T obj, string path)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                StreamWriter sw = File.CreateText(path);

                using (JsonWriter writerr = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writerr, obj);
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Error saveFile: " + ex);
            }
        }

        public static T ReadyFile<T>(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
                }
                else
                {
                    return default(T);
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Error saveFile: " + ex);
                return default(T);
            }
        }
    }
}
