using KeyStoneGraphQL.Application.Providers;
using System.Reflection;
using System.Text.Json;

namespace KeyStoneGraphQL.Tests
{
    public class DynamicTypeProviderTests
    {
        [Fact]
        public void CreateTypeFromJson_ValidJson_CreatesTypeWithCorrectProperties()
        {
            // Arrange
            var provider = new DynamicTypeProvider();
            var json = """
                {
                    "Id": 1,
                    "Name": "Test",
                    "IsActive": true,
                    "Score": 95.5
                }
                """;

            // Act
            var type = provider.CreateTypeFromJson(json, "TestType");

            // Assert
            Assert.NotNull(type);
            Assert.Equal("TestType", type.Name);
            
            var properties = type.GetProperties();
            Assert.Equal(4, properties.Length);
            
            Assert.Contains(properties, p => p.Name == "Id" && p.PropertyType == typeof(int));
            Assert.Contains(properties, p => p.Name == "Name" && p.PropertyType == typeof(string));
            Assert.Contains(properties, p => p.Name == "IsActive" && p.PropertyType == typeof(bool));
            Assert.Contains(properties, p => p.Name == "Score" && p.PropertyType == typeof(double));
        }

        [Fact]
        public void CreateInstanceFromJson_ValidJson_CreatesInstanceWithCorrectValues()
        {
            // Arrange
            var provider = new DynamicTypeProvider();
            var json = """
                {
                    "Id": 42,
                    "Name": "John Doe",
                    "IsActive": true,
                    "Score": 87.3
                }
                """;

            // Act
            var instance = provider.CreateInstanceFromJson(json, "Person");

            // Assert
            Assert.NotNull(instance);
            
            var type = instance.GetType();
            Assert.Equal("Person", type.Name);

            var idProperty = type.GetProperty("Id");
            Assert.Equal(42, idProperty?.GetValue(instance));

            var nameProperty = type.GetProperty("Name");
            Assert.Equal("John Doe", nameProperty?.GetValue(instance));

            var isActiveProperty = type.GetProperty("IsActive");
            Assert.Equal(true, isActiveProperty?.GetValue(instance));

            var scoreProperty = type.GetProperty("Score");
            Assert.Equal(87.3, scoreProperty?.GetValue(instance));
        }

        [Fact]
        public void CreateTypeFromJson_SameTypeName_ReturnsExistingType()
        {
            // Arrange
            var provider = new DynamicTypeProvider();
            var json1 = """{"Id": 1, "Name": "Test1"}""";
            var json2 = """{"Id": 2, "Name": "Test2"}""";

            // Act
            var type1 = provider.CreateTypeFromJson(json1, "SameType");
            var type2 = provider.CreateTypeFromJson(json2, "SameType");

            // Assert
            Assert.Same(type1, type2);
        }

        [Fact]
        public void CreateTypeFromJson_InvalidJson_ThrowsArgumentException()
        {
            // Arrange
            var provider = new DynamicTypeProvider();
            var invalidJson = "{ invalid json }";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => provider.CreateTypeFromJson(invalidJson, "InvalidType"));
        }

        [Fact]
        public void CreateTypeFromJson_EmptyJson_ThrowsArgumentException()
        {
            // Arrange
            var provider = new DynamicTypeProvider();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => provider.CreateTypeFromJson("", "EmptyType"));
            Assert.Throws<ArgumentException>(() => provider.CreateTypeFromJson(null, "NullType"));
        }

        [Fact]
        public void CreateTypeFromJson_NonObjectJson_ThrowsArgumentException()
        {
            // Arrange
            var provider = new DynamicTypeProvider();
            var arrayJson = """["item1", "item2"]""";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => provider.CreateTypeFromJson(arrayJson, "ArrayType"));
        }

        [Fact]
        public void GetDynamicTypes_AfterCreatingTypes_ReturnsAllCreatedTypes()
        {
            // Arrange
            var provider = new DynamicTypeProvider();
            var json1 = """{"Id": 1, "Name": "Type1"}""";
            var json2 = """{"Code": "ABC", "Value": 123}""";

            // Act
            provider.CreateTypeFromJson(json1, "FirstType");
            provider.CreateTypeFromJson(json2, "SecondType");
            var types = provider.GetDynamicTypes();

            // Assert
            Assert.Equal(2, types.Count());
            Assert.Contains(types, t => t.Name == "FirstType");
            Assert.Contains(types, t => t.Name == "SecondType");
        }

        [Fact]
        public void CreateTypeFromJson_ComplexDataTypes_HandlesCorrectly()
        {
            // Arrange
            var provider = new DynamicTypeProvider();
            var json = """
                {
                    "StringValue": "test string",
                    "IntValue": 42,
                    "DoubleValue": 3.14,
                    "BoolValue": false,
                    "NullValue": null
                }
                """;

            // Act
            var instance = provider.CreateInstanceFromJson(json, "ComplexType");

            // Assert
            var type = instance.GetType();
            
            var stringProp = type.GetProperty("StringValue");
            Assert.Equal(typeof(string), stringProp?.PropertyType);
            Assert.Equal("test string", stringProp?.GetValue(instance));

            var intProp = type.GetProperty("IntValue");
            Assert.Equal(typeof(int), intProp?.PropertyType);
            Assert.Equal(42, intProp?.GetValue(instance));

            var doubleProp = type.GetProperty("DoubleValue");
            Assert.Equal(typeof(double), doubleProp?.PropertyType);
            Assert.Equal(3.14, doubleProp?.GetValue(instance));

            var boolProp = type.GetProperty("BoolValue");
            Assert.Equal(typeof(bool), boolProp?.PropertyType);
            Assert.Equal(false, boolProp?.GetValue(instance));

            var nullProp = type.GetProperty("NullValue");
            Assert.Equal(typeof(object), nullProp?.PropertyType);
            Assert.Null(nullProp?.GetValue(instance));
        }

        [Fact]
        public void CreateTypeFromJson_CanModifyProperties_AfterCreation()
        {
            // Arrange
            var provider = new DynamicTypeProvider();
            var json = """{"Name": "Original", "Value": 100}""";

            // Act
            var instance = provider.CreateInstanceFromJson(json, "ModifiableType");
            var type = instance.GetType();

            var nameProperty = type.GetProperty("Name");
            var valueProperty = type.GetProperty("Value");

            // Modify properties
            nameProperty?.SetValue(instance, "Modified");
            valueProperty?.SetValue(instance, 200);

            // Assert
            Assert.Equal("Modified", nameProperty?.GetValue(instance));
            Assert.Equal(200, valueProperty?.GetValue(instance));
        }

        [Theory]
        [InlineData("""{"Count": 42}""", "Count", typeof(int), 42)]
        [InlineData("""{"Rate": 3.14}""", "Rate", typeof(double), 3.14)]
        [InlineData("""{"Name": "Test"}""", "Name", typeof(string), "Test")]
        [InlineData("""{"Active": true}""", "Active", typeof(bool), true)]
        public void CreateTypeFromJson_VariousDataTypes_CreatesCorrectPropertyTypes(
            string json, string propertyName, Type expectedType, object expectedValue)
        {
            // Arrange
            var provider = new DynamicTypeProvider();

            // Act
            var instance = provider.CreateInstanceFromJson(json, "TestType");
            var type = instance.GetType();
            var property = type.GetProperty(propertyName);

            // Assert
            Assert.NotNull(property);
            Assert.Equal(expectedType, property.PropertyType);
            Assert.Equal(expectedValue, property.GetValue(instance));
        }
    }
}
