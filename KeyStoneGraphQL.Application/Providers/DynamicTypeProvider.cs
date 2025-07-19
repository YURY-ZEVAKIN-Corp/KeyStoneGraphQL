using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;

namespace KeyStoneGraphQL.Application.Providers
{
    // Dynamic type provider that creates types from JSON
    public class DynamicTypeProvider
    {
        private readonly ModuleBuilder _moduleBuilder;
        private readonly Dictionary<string, Type> _createdTypes;

        public DynamicTypeProvider()
        {
            var assemblyName = new AssemblyName("DynamicTypesAssembly");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            _moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicTypesModule");
            _createdTypes = new Dictionary<string, Type>();
        }

        public IEnumerable<Type> GetDynamicTypes()
        {
            return _createdTypes.Values;
        }

        /// <summary>
        /// Creates a dynamic type from JSON text
        /// </summary>
        /// <param name="jsonText">JSON string to analyze</param>
        /// <param name="typeName">Name for the generated type</param>
        /// <returns>Generated Type</returns>
        public Type CreateTypeFromJson(string jsonText, string typeName = "DynamicType")
        {
            if (string.IsNullOrWhiteSpace(jsonText))
                throw new ArgumentException("JSON text cannot be null or empty", nameof(jsonText));

            // Check if type already exists
            if (_createdTypes.ContainsKey(typeName))
                return _createdTypes[typeName];

            try
            {
                // Parse JSON to get structure
                using var document = JsonDocument.Parse(jsonText);
                var rootElement = document.RootElement;

                // Create the dynamic type
                var type = CreateTypeFromJsonElement(rootElement, typeName);
                _createdTypes[typeName] = type;
                return type;
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"Invalid JSON: {ex.Message}", nameof(jsonText), ex);
            }
        }

        /// <summary>
        /// Creates an instance of the dynamic type and populates it with JSON data
        /// </summary>
        /// <param name="jsonText">JSON string</param>
        /// <param name="typeName">Type name to create instance of</param>
        /// <returns>Instance of the dynamic type</returns>
        public object CreateInstanceFromJson(string jsonText, string typeName = "DynamicType")
        {
            var type = CreateTypeFromJson(jsonText, typeName);
            var instance = Activator.CreateInstance(type);

            PopulateInstanceFromJson(instance, jsonText);
            return instance;
        }

        private Type CreateTypeFromJsonElement(JsonElement element, string typeName)
        {
            if (element.ValueKind != JsonValueKind.Object)
                throw new ArgumentException("Root JSON element must be an object");

            var typeBuilder = _moduleBuilder.DefineType(typeName, 
                TypeAttributes.Public | TypeAttributes.Class);

            // Add parameterless constructor
            CreateParameterlessConstructor(typeBuilder);

            // Add properties based on JSON structure
            foreach (var property in element.EnumerateObject())
            {
                var propertyType = GetClrTypeFromJsonElement(property.Value);
                CreateProperty(typeBuilder, property.Name, propertyType);
            }

            return typeBuilder.CreateType();
        }

        private void CreateParameterlessConstructor(TypeBuilder typeBuilder)
        {
            var constructorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                Type.EmptyTypes);

            var ilGenerator = constructorBuilder.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));
            ilGenerator.Emit(OpCodes.Ret);
        }

        private void CreateProperty(TypeBuilder typeBuilder, string propertyName, Type propertyType)
        {
            // Create backing field
            var fieldBuilder = typeBuilder.DefineField($"_{propertyName.ToLowerInvariant()}", 
                propertyType, FieldAttributes.Private);

            // Create property
            var propertyBuilder = typeBuilder.DefineProperty(propertyName, 
                PropertyAttributes.HasDefault, propertyType, null);

            // Create getter
            var getterBuilder = typeBuilder.DefineMethod($"get_{propertyName}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                propertyType, Type.EmptyTypes);

            var getterIl = getterBuilder.GetILGenerator();
            getterIl.Emit(OpCodes.Ldarg_0);
            getterIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getterIl.Emit(OpCodes.Ret);

            // Create setter
            var setterBuilder = typeBuilder.DefineMethod($"set_{propertyName}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                null, new[] { propertyType });

            var setterIl = setterBuilder.GetILGenerator();
            setterIl.Emit(OpCodes.Ldarg_0);
            setterIl.Emit(OpCodes.Ldarg_1);
            setterIl.Emit(OpCodes.Stfld, fieldBuilder);
            setterIl.Emit(OpCodes.Ret);

            // Set the get/set methods
            propertyBuilder.SetGetMethod(getterBuilder);
            propertyBuilder.SetSetMethod(setterBuilder);
        }

        private Type GetClrTypeFromJsonElement(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => typeof(string),
                JsonValueKind.Number => element.TryGetInt32(out _) ? typeof(int) : typeof(double),
                JsonValueKind.True or JsonValueKind.False => typeof(bool),
                JsonValueKind.Array => typeof(object[]),
                JsonValueKind.Object => typeof(object),
                JsonValueKind.Null => typeof(object),
                _ => typeof(object)
            };
        }

        private void PopulateInstanceFromJson(object instance, string jsonText)
        {
            using var document = JsonDocument.Parse(jsonText);
            var rootElement = document.RootElement;

            var type = instance.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                if (rootElement.TryGetProperty(property.Name, out var jsonProperty))
                {
                    var value = ConvertJsonValueToClrValue(jsonProperty, property.PropertyType);
                    property.SetValue(instance, value);
                }
            }
        }

        private object ConvertJsonValueToClrValue(JsonElement element, Type targetType)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number when targetType == typeof(int) => element.GetInt32(),
                JsonValueKind.Number when targetType == typeof(double) => element.GetDouble(),
                JsonValueKind.Number when targetType == typeof(decimal) => element.GetDecimal(),
                JsonValueKind.Number => element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => element.GetRawText()
            };
        }

    }
}
