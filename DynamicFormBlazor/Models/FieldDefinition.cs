namespace DynamicFormBlazor.Models;

public abstract class FieldDefinition
{
    public string Key { get; set; }
    public string Label { get; set; }
    public abstract Type ValueType { get; }
}

public class FieldDefinition<T> : FieldDefinition
{
    public T Value { get; set; }
    public override Type ValueType => typeof(T);
}

public class SelectFieldDefinition<T> : FieldDefinition<T>
{
    public IEnumerable<FieldOption<T>> Options { get; set; } = [];
}

public class FieldOption<T>
{
    public T Value { get; set; }
    public string Label { get; set; }
}