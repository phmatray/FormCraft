using System.Text;
using FormCraft;

namespace FormCraft.DemoBlazorApp.Services;

public class FormCodeGeneratorService
{
    public string GenerateFormBuilderCode<TModel>(IFormConfiguration<TModel> configuration) where TModel : class, new()
    {
        var sb = new StringBuilder();
        var modelType = typeof(TModel).Name;

        sb.AppendLine($"var formConfig = FormBuilder<{modelType}>");
        sb.AppendLine($"    .Create()");

        // Add layout if not default
        if (configuration.Layout != FormLayout.Vertical)
        {
            sb.AppendLine($"    .WithLayout(FormLayout.{configuration.Layout})");
        }

        // Group fields by group if using field groups
        if (configuration is IGroupedFormConfiguration<TModel> groupedConfig && groupedConfig.UseFieldGroups)
        {
            foreach (var group in groupedConfig.FieldGroups.OrderBy(g => g.Order))
            {
                sb.AppendLine($"    .AddFieldGroup(group => group");

                if (!string.IsNullOrEmpty(group.Name))
                    sb.AppendLine($"        .WithGroupName(\"{group.Name}\")");

                if (group.Columns > 1)
                    sb.AppendLine($"        .WithColumns({group.Columns})");

                if (group.ShowCard)
                {
                    if (group.CardElevation != 1)
                        sb.AppendLine($"        .ShowInCard({group.CardElevation})");
                    else
                        sb.AppendLine($"        .ShowInCard()");
                }

                foreach (var fieldName in group.FieldNames)
                {
                    var field = configuration.Fields.FirstOrDefault(f => f.FieldName == fieldName);
                    if (field != null)
                    {
                        AppendFieldConfiguration(sb, field, "        ", true);
                    }
                }

                sb.AppendLine($"    )");
            }
        }
        else
        {
            // Add regular fields
            foreach (var field in configuration.Fields)
            {
                AppendFieldConfiguration(sb, field, "    ", false);
            }
        }

        sb.Append($"    .Build();");

        return sb.ToString();
    }

    private void AppendFieldConfiguration<TModel>(StringBuilder sb, IFieldConfiguration<TModel, object> field, string indent, bool inGroup)
        where TModel : class, new()
    {
        if (inGroup)
        {
            sb.AppendLine($"{indent}.AddField(x => x.{field.FieldName}, field => field");
            var fieldIndent = indent + "    ";

            // Add field configuration methods
            if (!string.IsNullOrEmpty(field.Label) && field.Label != field.FieldName)
                sb.AppendLine($"{fieldIndent}.WithLabel(\"{field.Label}\")");

            if (field.IsRequired)
                sb.AppendLine($"{fieldIndent}.Required()");

            if (!string.IsNullOrEmpty(field.Placeholder))
                sb.AppendLine($"{fieldIndent}.WithPlaceholder(\"{field.Placeholder}\")");

            if (!string.IsNullOrEmpty(field.HelpText))
                sb.AppendLine($"{fieldIndent}.WithHelpText(\"{field.HelpText}\")");

            // Add custom renderer if specified
            if (field.CustomRendererType != null)
            {
                // Get the field type from the expression
                var property = typeof(TModel).GetProperty(field.FieldName);
                var fieldType = property?.PropertyType.Name ?? "object";
                var rendererType = field.CustomRendererType.Name;
                sb.AppendLine($"{fieldIndent}.WithCustomRenderer<{typeof(TModel).Name}, {fieldType}, {rendererType}>()");
            }

            // Add text area configuration if Lines attribute is present
            if (field.AdditionalAttributes.TryGetValue("Lines", out var linesObj) && linesObj is int lines)
            {
                sb.AppendLine($"{fieldIndent}.AsTextArea(lines: {lines})");
            }

            // Add select options if available
            AppendSelectOptions(sb, field, fieldIndent);

            // Remove the last line break to close the parenthesis properly
            if (sb.Length > 2 && sb[sb.Length - 2] == '\r' && sb[sb.Length - 1] == '\n')
                sb.Length -= 2;
            else if (sb.Length > 1 && sb[sb.Length - 1] == '\n')
                sb.Length -= 1;

            sb.AppendLine($")");
        }
        else
        {
            sb.AppendLine($"{indent}.AddField(x => x.{field.FieldName})");
            var fieldIndent = indent + "    ";

            if (!string.IsNullOrEmpty(field.Label) && field.Label != field.FieldName)
                sb.AppendLine($"{fieldIndent}.WithLabel(\"{field.Label}\")");

            if (field.IsRequired)
                sb.AppendLine($"{fieldIndent}.Required()");

            if (!string.IsNullOrEmpty(field.Placeholder))
                sb.AppendLine($"{fieldIndent}.WithPlaceholder(\"{field.Placeholder}\")");

            if (!string.IsNullOrEmpty(field.HelpText))
                sb.AppendLine($"{fieldIndent}.WithHelpText(\"{field.HelpText}\")");

            AppendSelectOptions(sb, field, fieldIndent);
        }
    }

    private void AppendSelectOptions<TModel>(StringBuilder sb, IFieldConfiguration<TModel, object> field, string indent)
        where TModel : class, new()
    {
        if (field.AdditionalAttributes.TryGetValue("Options", out var optionsObj))
        {
            if (optionsObj is IEnumerable<SelectOption<string>> stringOptions)
            {
                var optionsList = stringOptions.ToList();
                if (optionsList.Any())
                {
                    sb.AppendLine($"{indent}.WithOptions(");
                    for (int i = 0; i < optionsList.Count; i++)
                    {
                        var opt = optionsList[i];
                        var comma = i < optionsList.Count - 1 ? "," : "";
                        sb.AppendLine($"{indent}    (\"{opt.Value}\", \"{opt.Label}\"){comma}");
                    }
                    sb.AppendLine($"{indent})");
                }
            }
        }
    }
}