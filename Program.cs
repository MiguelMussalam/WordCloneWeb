using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using System;
using System.Collections.Generic;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Enable serving static files from wwwroot folder (needed for our HTML frontend)
app.UseDefaultFiles();
app.UseStaticFiles();

// --- IN-MEMORY DATABASE: Simulate a repository for MVP ---
var documentStore = new List<DocumentEntity>();

// GET all documents
app.MapGet("/api/documents", () =>
{
    return Results.Ok(documentStore.OrderByDescending(d => d.CreatedAt));
});

// GET specific document by id
app.MapGet("/api/documents/{id}", (Guid id) =>
{
    var doc = documentStore.FirstOrDefault(d => d.Id == id);
    if (doc is null) return Results.NotFound();

    // Enhancing the response payload with the fully rendered wrapper
    // so the Frontend can display it exactly as it will print!
    return Results.Ok(new {
        Id = doc.Id,
        Title = doc.Title,
        Status = doc.Status,
        RequiresSignature = doc.RequiresSignature,
        IsSigned = doc.IsSigned,
        CreatedAt = doc.CreatedAt,
        // The raw HTML typed by the user
        HtmlContent = doc.HtmlContent,
        // The exact formatted page with Corporate Letterhead
        WrappedHtmlContent = DocumentFormatter.WrapInCorporateTemplate(doc)
    });
});

// POST to create a new document
app.MapPost("/api/documents", ([FromBody] CreateDocumentRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.HtmlContent))
        return Results.BadRequest("O conteúdo não pode ser vazio.");

    // Generate Sequential ID based on date: YYYYMMDD-001
    var todayStr = DateTime.Now.ToString("yyyyMMdd");
    var todaysDocsCount = documentStore.Count(d => d.Title.StartsWith($"OF-{todayStr}"));
    var nextSeq = todaysDocsCount + 1;
    var autoTitle = $"OF-{todayStr}-{nextSeq:D3}";

    var newDoc = new DocumentEntity
    {
        Title = autoTitle,
        HtmlContent = request.HtmlContent,
        RequiresSignature = request.RequiresSignature,
        Status = "PendingValidation"
    };

    documentStore.Add(newDoc);
    return Results.Created($"/api/documents/{newDoc.Id}", newDoc);
});

// PUT to validate (Approve/Reject) a document
app.MapPut("/api/documents/{id}/validate", (Guid id, [FromBody] ValidateDocumentRequest request) =>
{
    var doc = documentStore.FirstOrDefault(d => d.Id == id);
    if (doc == null) return Results.NotFound();

    if (doc.Status != "PendingValidation")
        return Results.BadRequest("Documento não está mais pendente de validação.");

    if (request.Approved)
    {
        doc.Status = "Approved";
    }
    else
    {
        doc.Status = "Rejected";
        doc.RejectionComment = request.Comment;
    }

    return Results.Ok(doc);
});

// PUT to sign a document
app.MapPut("/api/documents/{id}/sign", (Guid id) =>
{
    var doc = documentStore.FirstOrDefault(d => d.Id == id);
    if (doc == null) return Results.NotFound();

    if (doc.Status != "Approved")
        return Results.BadRequest("Documento não foi aprovado pelo jurídico.");

    if (!doc.RequiresSignature)
        return Results.BadRequest("Este documento não requer assinatura.");

    doc.IsSigned = true;
    doc.Status = "FullySigned";

    return Results.Ok(doc);
});

// Original logic transformed into an HTTP GET that renders the PDF of an already approved/signed document
app.MapGet("/api/documents/{id}/pdf", async (Guid id) =>
{
    var doc = documentStore.FirstOrDefault(d => d.Id == id);
    if (doc == null) return Results.NotFound();

    // In a real app, only FullySigned or Approved documents should be printed.
    // For this MVP, we allow printing any document to test visual output.

    // Retrive the shared exact template HTML
    var templateHtml = DocumentFormatter.WrapInCorporateTemplate(doc);

    var browserFetcher = new BrowserFetcher();
    await browserFetcher.DownloadAsync();
    await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
    await using var page = await browser.NewPageAsync();
    await page.SetContentAsync(templateHtml);
    var pdfData = await page.PdfDataAsync(new PdfOptions
    {
        Format = PaperFormat.A4,
        PrintBackground = true,
        MarginOptions = new MarginOptions { Top = "0", Bottom = "0", Left = "0", Right = "0" }
    });

    return Results.File(pdfData, "application/pdf", $"Oficio_{doc.Id.ToString().Substring(0,8)}.pdf");
});

app.Run();

// Entities & DTOs
public class DocumentEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public string Status { get; set; } = "PendingValidation"; // PendingValidation, Approved, Rejected, FullySigned
    public string? RejectionComment { get; set; }
    public bool RequiresSignature { get; set; }
    public bool IsSigned { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class CreateDocumentRequest
{
    public string HtmlContent { get; set; } = string.Empty;
    public bool RequiresSignature { get; set; }
}

public class ValidateDocumentRequest
{
    public bool Approved { get; set; }
    public string? Comment { get; set; }
}

// --- Utilities for Shared Document Formatting (Must be at the bottom due to Top-level statements) ---
public static class DocumentFormatter
{
    public static string WrapInCorporateTemplate(DocumentEntity doc)
    {
        var signatureHtml = string.Empty;
        if (doc.RequiresSignature)
        {
            var mark = doc.IsSigned ? "(Assinado Digitalmente)" : "(Aguardando Assinatura)";
            var color = doc.IsSigned ? "green" : "red";
            
            signatureHtml = $@"
                <div class='signatures'>
                    <div class='signature-block'>
                        <div class='signature-line'></div>
                        Diretor de Operações<br>
                        <strong>João da Silva</strong><br>
                        <span style='color:{color}; font-size:10px;'>{mark}</span>
                    </div>
                    <!-- Simulating a generic required second signature -->
                    <div class='signature-block'>
                        <div class='signature-line'></div>
                        Gerente de Contratos<br>
                        <strong>Maria Souza</strong><br>
                        <span style='color:{color}; font-size:10px;'>{mark}</span>
                    </div>
                </div>";
        }

        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <style>
                    @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;700&display=swap');
                    body {{ font-family: 'Inter', Arial, sans-serif; margin: 0; padding: 0; color: #333; min-height: 100vh; display: flex; align-items: center; justify-content: center; background:#ccc;}}
                    /* Escala para 100% no iframe da validação */
                    .page {{ padding: 2cm; box-sizing: border-box; background: white; width: 100%; min-height: 29.7cm; margin: 0 auto; position: relative; }}
                    .header {{ border-bottom: 3px solid #004488; padding-bottom: 15px; margin-bottom: 30px; display: flex; justify-content: space-between; align-items: flex-end; }}
                    .logo {{ font-size: 28px; font-weight: 700; color: #004488; letter-spacing: -1px; }}
                    .company-info {{ text-align: right; font-size: 11px; color: #666; line-height: 1.4; }}
                    .content {{ font-size: 14px; line-height: 1.6; min-height: 500px; text-align: justify; }}
                    .content h2 {{ color: #004488; font-size: 18px; margin-top: 20px; }}
                    .content h3 {{ color: #222; font-size: 16px; }}
                    .content p {{ margin-bottom: 15px; }}
                    .content img {{ max-width: 100%; height: auto; display: block; margin: 15px auto; border-radius: 4px; }}
                    .content figure.image {{ display: table; clear: both; text-align: center; margin: 0.9em auto; }}
                    
                    .signatures {{ display: flex; justify-content: space-around; margin-top: 60px; page-break-inside: avoid; }}
                    .signature-block {{ text-align: center; font-size: 12px; width: 40%; }}
                    .signature-line {{ border-top: 1px solid #333; margin-bottom: 10px; height: 1px; }}
                    
                    .footer {{ border-top: 1px solid #ddd; padding-top: 15px; margin-top: 40px; text-align: center; font-size: 10px; color: #888; position: relative; bottom: 0; width:100%;}}
                    .watermark {{ position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%) rotate(-45deg); font-size: 100px; color: rgba(0, 68, 136, 0.05); z-index: 0; font-weight: bold; white-space: nowrap; pointer-events: none; }}
                </style>
            </head>
            <body>
                <div class='page'>
                    <div class='watermark'>USO INTERNO - ACME</div>
                    <div class='header'>
                        <div class='logo'>ACME Corp.</div>
                        <div class='company-info'>
                            Edifício Corporate Plaza<br>Av. Paulista, 1000 - São Paulo, SP<br>CNPJ: 00.000.000/0001-00<br>contato@acmecorp.com.br
                        </div>
                    </div>
                    
                    <h3 style='text-align:center;'>{doc.Title}</h3>
                    <div class='content' style='position:relative; z-index: 1;'>
                        {doc.HtmlContent}
                    </div>

                    {signatureHtml}

                    <div class='footer'>
                        Documento gerado eletronicamente em {DateTime.Now:dd/MM/yyyy HH:mm:ss} - Válido em todo o território nacional.<br>
                        <strong>ACME Corporation &copy; {DateTime.Now.Year}</strong><br>
                        ID: {doc.Id}
                    </div>
                </div>
            </body>
            </html>
        ";
    }
}
