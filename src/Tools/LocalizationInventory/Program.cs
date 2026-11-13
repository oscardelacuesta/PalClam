// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;
if(args.Length!=2) { Console.Error.WriteLine("Usage: LocalizationInventory <source-folder> <output.json>"); Environment.ExitCode=2; return; }
var root = args[0];
var output = args[1];
var values = new Dictionary<string,Item>(StringComparer.Ordinal);
var syntaxErrors = new List<string>();
foreach(var file in Directory.EnumerateFiles(root,"*.cs",SearchOption.AllDirectories).Where(x=>!x.Contains("/obj/")&&!x.Contains("/bin/")&&!x.Contains("/Localization/")&&!x.Contains("/UI/LocalizedUi.cs"))) {
 var tree=CSharpSyntaxTree.ParseText(File.ReadAllText(file),new CSharpParseOptions(LanguageVersion.Preview));
 foreach(var error in tree.GetDiagnostics().Where(x=>x.Severity==DiagnosticSeverity.Error))syntaxErrors.Add(file+":"+error);
 foreach(var node in tree.GetRoot().DescendantNodes().OfType<ExpressionSyntax>()) {
  if(node is not LiteralExpressionSyntax and not InterpolatedStringExpressionSyntax && !(node is BinaryExpressionSyntax bin && bin.IsKind(SyntaxKind.AddExpression)))continue;
  if(node is LiteralExpressionSyntax literal && !literal.IsKind(SyntaxKind.StringLiteralExpression))continue;
  if(node.Parent is BinaryExpressionSyntax parent && parent.IsKind(SyntaxKind.AddExpression))continue;
  var variables=new List<string>();
  var text=Make(node,variables);
  if(!Natural(text) && !UiContext(node))continue;
  if(text.Length>2500)continue;
  var loc=new LocationItem(Path.GetRelativePath(root,file),tree.GetLineSpan(node.Span).StartLinePosition.Line+1,node.Span.Start,node.Span.Length,variables);
  if(!values.TryGetValue(text,out var item)) {
   var key="Display."+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant()[..12];
   values[text]=item=new Item(key,text,new List<LocationItem>());
  }
  item.Locations.Add(loc);
 }
}
File.WriteAllText(output,JsonSerializer.Serialize(values.Values.OrderBy(x=>x.Key),new JsonSerializerOptions{WriteIndented=true,Encoder=System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping}));
Console.WriteLine($"{values.Count} templates; {values.Keys.Sum(x=>x.Length)} characters; {syntaxErrors.Count} syntax errors");
foreach(var error in syntaxErrors.Take(10))Console.WriteLine(error);
static bool HasString(ExpressionSyntax e)=>e.DescendantNodesAndSelf().Any(n=>n is InterpolatedStringExpressionSyntax || n is LiteralExpressionSyntax s && s.IsKind(SyntaxKind.StringLiteralExpression));
static string Make(ExpressionSyntax expression,List<string> args) {
 if(expression is LiteralExpressionSyntax lit && lit.IsKind(SyntaxKind.StringLiteralExpression))return lit.Token.ValueText.Replace("{","{{").Replace("}","}}");
 if(expression is InterpolatedStringExpressionSyntax interpolated) {
  var sb=new StringBuilder();
  foreach(var content in interpolated.Contents) {
   if(content is InterpolatedStringTextSyntax text)sb.Append(text.TextToken.ValueText.Replace("{","{{").Replace("}","}}"));
   else if(content is InterpolationSyntax value) {
    var index=args.Count;args.Add(value.Expression.ToString());
    sb.Append('{').Append(index);
    if(value.AlignmentClause is not null)sb.Append(',').Append(value.AlignmentClause.Value);
    if(value.FormatClause is not null)sb.Append(':').Append(value.FormatClause.FormatStringToken.ValueText);
    sb.Append('}');
   }
  }
  return sb.ToString();
 }
 if(expression is BinaryExpressionSyntax add && add.IsKind(SyntaxKind.AddExpression) && HasString(add))return Make(add.Left,args)+Make(add.Right,args);
 if(expression is ParenthesizedExpressionSyntax par && par.Expression is BinaryExpressionSyntax pb && pb.IsKind(SyntaxKind.AddExpression) && HasString(pb))return Make(pb,args);
 var id=args.Count;args.Add(expression.ToString());return "{"+id+"}";
}
static bool Natural(string value) {
 if(!Regex.IsMatch(value,@"[a-zA-ZáéíóúñÁÉÍÓÚÑ]"))return false;
 if(Regex.IsMatch(value,@"^(?:https?://|--|#?[A-Fa-f0-9]{6,8}$|[\w.-]+\.(?:json|exe|txt|csv|md|conf|log|ps1|dll|resx)$)"))return false;
 return Regex.IsMatch(value,@"[áéíóúñÁÉÍÓÚÑ]|\b(?:archivos?|actualiza\w*|carpetas?|elegir|comprobar|analiza\w*|seguridad|motor|local|error|no|sin|para|se|un|con|la|el|de|opcional|privacidad|estado|activ\w*|ver|instala\w*|protecci\w*|correo|herramientas|descargas|equilibrado|prioritario|general|pausado|detenido|inactivo|reiniciar|guardar|firmas|restaurar|borrar|eliminar|reanudar|abrir|filtro|todos|limpiar|detener|pausar|entrada|salida|nivel|cargando|pendiente|morado|naranja|rojo|verde|azul|suave|din.mica|historial|cuarentena|rendimiento|resumen|cancelado|cancelar|ayuda|autor|licencia|apariencia|notificaciones|administrador|resultados|operativo|revisar|mostrar|copiar|exportar|aplicar|selecci\w*|amenazas|destino|tipo|ruta|fecha|hora|idioma|tema|oscuro|violeta|crema|animad\w*|vista|cierre|confirm\w*|requerido|opciones|r.pido|procesa\w*|equipo|disponible|complet\w*|continuar|lista|prueba|unidad\w*|memoria|ventana|latido|vigilancia|fallo)\b",RegexOptions.IgnoreCase);
}
static bool UiContext(ExpressionSyntax node) {
 foreach(var ancestor in node.Ancestors()) {
  if(ancestor is PropertyDeclarationSyntax property && Regex.IsMatch(property.Identifier.Text,@"Display|Label|Status|State|Outcome|Review|Description"))return true;
  if(ancestor is MethodDeclarationSyntax method && Regex.IsMatch(method.Identifier.Text,@"(?:Text|Summary|Description|Status)$"))return true;
  if(ancestor is AssignmentExpressionSyntax assignment && assignment.Left.ToString().Split('.').Last() is "Text" or "Header" or "Title" or "ToolTip" or "Content")return true;
  if(ancestor is ArgumentSyntax argument && argument.Parent is ArgumentListSyntax list && list.Parent is InvocationExpressionSyntax call) {
   var name=call.Expression.ToString().Split('.').Last();var index=list.Arguments.IndexOf(argument);
   if((name is "T" or "Text" or "Input" or "ActionButton" or "OptionCheck" or "Metric" or "MetricCard" or "Column") && index==0)return true;
   if(name=="OptionCheck" && index==2)return true;
   if(name=="Nav" && index==1 || name=="NavGroup" && index==0)return true;
  }
 }
 return false;
}
record LocationItem(string File,int Line,int Start,int Length,List<string> Arguments);
record Item(string Key,string Source,List<LocationItem> Locations);

