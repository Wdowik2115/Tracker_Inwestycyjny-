import { Pipe, PipeTransform, SecurityContext, inject } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { marked } from 'marked';
import dompurify from 'dompurify';

@Pipe({
  name: 'markdown',
  standalone: true
})
export class MarkdownPipe implements PipeTransform {
  private sanitizer = inject(DomSanitizer);

  async transform(value: string | undefined): Promise<SafeHtml> {
    if (!value) return '';
    
    // Parse markdown to HTML
    const rawHtml = await marked.parse(value);
    
    // Sanitize HTML to prevent XSS
    const sanitizedHtml = dompurify.sanitize(rawHtml);
    
    // Tell Angular it's safe to render
    return this.sanitizer.bypassSecurityTrustHtml(sanitizedHtml);
  }
}
