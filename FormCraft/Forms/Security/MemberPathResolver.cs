using System.Linq.Expressions;
using System.Reflection;

namespace FormCraft;

/// <summary>
/// Turns a member-access lambda into its full dotted path (<c>x =&gt; x.Address.City</c> →
/// <c>"Address.City"</c>) and resolves such a path back to the property chain it names (#423).
/// </summary>
/// <remarks>
/// <para>
/// One path format serves every security consumer: <c>SecurityBuilder.EncryptField</c> stores it in
/// <see cref="IFormSecurity.EncryptedFields"/>, <see cref="FormSecurityEnforcer{TModel}"/> keys audit
/// entries by it (#406), and <see cref="EncryptedFieldHelper"/> /
/// <see cref="EncryptionServiceExtensions"/> resolve it to reach the value. Before #423 the first stored
/// only the expression's last member (<c>"City"</c>), which the other consumers looked up with
/// <c>typeof(TModel).GetProperty(name)</c> — a lookup that returns <see langword="null"/> for every
/// nested field, so the field was silently never encrypted.
/// </para>
/// <para>
/// Why not <see cref="FieldValueGetterCache{TModel}"/> / <see cref="FieldValueSetterCache{TModel}"/>:
/// both are keyed on an <see cref="IFieldConfiguration{TModel, TValue}"/> instance, while every
/// consumer here holds only the <see cref="string"/> in <see cref="IFormSecurity.EncryptedFields"/> —
/// a public, mutable set applications populate by hand. String-to-member resolution is therefore
/// needed regardless, and it runs once per submit, so a compiled-delegate cache would buy nothing.
/// </para>
/// </remarks>
internal static class MemberPathResolver
{
    /// <summary>
    /// Returns the dotted member path of <paramref name="expression"/>, or <see langword="null"/> when
    /// its body is not a member-access chain rooted at the lambda's own parameter (a leading
    /// <c>Convert</c>, as <see cref="FieldConfigurationWrapper{TModel, TValue}"/> adds, is unwrapped).
    /// </summary>
    internal static string? GetDottedPath(LambdaExpression expression)
    {
        var body = expression.Body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert
            ? convert.Operand
            : expression.Body;

        var segments = new Stack<string>();
        while (body is MemberExpression member)
        {
            segments.Push(member.Member.Name);
            body = member.Expression;
        }

        return segments.Count > 0 && body == expression.Parameters[0] ? string.Join(".", segments) : null;
    }

    /// <summary>
    /// Resolves <paramref name="dottedPath"/> against <paramref name="rootType"/> to the chain of
    /// public, readable instance properties it names, ending in a <see cref="string"/>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// A segment is not such a property, or the last one is not a <see cref="string"/>. Encryption
    /// fails closed: a configured field that cannot be reached must never be skipped silently, since
    /// skipping it leaves the value in plaintext.
    /// </exception>
    internal static PropertyInfo[] ResolveStringProperty(Type rootType, string dottedPath)
    {
        var segments = dottedPath.Split('.');
        var chain = new PropertyInfo[segments.Length];
        var type = rootType;
        for (var i = 0; i < segments.Length; i++)
        {
            PropertyInfo? property;
            try
            {
                property = type.GetProperty(segments[i], BindingFlags.Public | BindingFlags.Instance);
            }
            catch (AmbiguousMatchException)
            {
                property = null; // e.g. a property hidden with `new`: report it like any other miss.
            }

            // Type.GetProperty only looks at members declared directly on the queried type (or
            // inherited through a class hierarchy) — it does not search the interfaces a type
            // implements. So an interface-typed intermediate member that only *inherits* the target
            // property from a base interface (#429) needs its own search across `GetInterfaces()`.
            if (property is null && type.IsInterface)
            {
                property = ResolveFromBaseInterfaces(type, segments[i]);
            }

            if (property is not { CanRead: true })
            {
                throw new ArgumentException(
                    $"Encrypted field '{dottedPath}' cannot be resolved on {rootType.Name}: '{segments[i]}' is not a readable public property of {type.Name}.");
            }

            chain[i] = property;
            type = property.PropertyType;
        }

        if (type != typeof(string))
        {
            throw new ArgumentException(
                $"Encrypted field '{dottedPath}' on {rootType.Name} is of type {type.Name}; only string members can be encrypted.");
        }

        return chain;
    }

    /// <summary>
    /// Searches every interface <paramref name="interfaceType"/> inherits from (transitively, via
    /// <see cref="Type.GetInterfaces"/>) for a public, instance property named <paramref name="name"/>.
    /// </summary>
    /// <remarks>
    /// Two distinct base interfaces declaring a same-named property is treated the same as
    /// <see cref="AmbiguousMatchException"/> above: fail closed (return <see langword="null"/>) rather
    /// than guess. A diamond that converges on the *same* interface is not ambiguous —
    /// <see cref="Type.GetInterfaces"/> already de-duplicates it to one entry.
    /// </remarks>
    private static PropertyInfo? ResolveFromBaseInterfaces(Type interfaceType, string name)
    {
        PropertyInfo? found = null;
        foreach (var baseInterface in interfaceType.GetInterfaces())
        {
            PropertyInfo? candidate;
            try
            {
                candidate = baseInterface.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            }
            catch (AmbiguousMatchException)
            {
                return null;
            }

            if (candidate is null)
            {
                continue;
            }

            if (found is not null && found.DeclaringType != candidate.DeclaringType)
            {
                return null; // Two unrelated base interfaces both declare `name`: genuinely ambiguous.
            }

            found = candidate;
        }

        return found;
    }

    /// <summary>
    /// Walks <paramref name="root"/> down to the object owning <paramref name="chain"/>'s last
    /// property. Returns <see langword="false"/> when an intermediate is <see langword="null"/> — there
    /// is then no value to encrypt.
    /// </summary>
    internal static bool TryGetOwner(object root, PropertyInfo[] chain, out object owner)
    {
        owner = root;
        for (var i = 0; i < chain.Length - 1; i++)
        {
            if (chain[i].GetValue(owner) is not { } next)
            {
                return false;
            }

            owner = next;
        }

        return true;
    }
}
