import { Component, OnInit, ViewChild, ElementRef, AfterViewChecked, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { toObservable } from '@angular/core/rxjs-interop';
import { ChatService } from '../../services/chat.service';
import { ChatMessageDto, AuthResponseDto } from '../../models';
import { AuthService } from '../../services/auth.service';

import { MarkdownPipe } from '../../utils/pipes/markdown.pipe';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, FormsModule, MarkdownPipe],
  templateUrl: './chat.html',
  styleUrls: ['./chat.css']
})
export class ChatComponent implements OnInit, AfterViewChecked {
  @ViewChild('scrollContainer') private scrollContainer!: ElementRef;

  private chatService = inject(ChatService);
  public authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);

  isOpen = false;
  messages: ChatMessageDto[] = [];
  newMessage = '';
  isLoading = false;
  showChoicePrompt = false;
  isSlowResponse = false;
  private slowResponseTimer: any;

  suggestions = [
    { label: '💰 Mój portfel', text: 'Jakie aktywa mam w swoim portfelu i jaka jest ich wartość?' },
    { label: '📈 Zysk/Strata', text: 'Jaki jest mój obecny zysk lub strata (P&L)?' },
    { label: '🚀 Trendy BTC', text: 'Jaka jest aktualna cena Bitcoina i jak zmieniła się w ostatnim czasie?' },
    { label: '🛡️ Porada', text: 'Daj mi krótką poradę dotyczącą zarządzania portfelem kryptowalut.' }
  ];

  constructor() {
    // Subscribe to auth changes in constructor (injection context)
    toObservable(this.authService.isAuthenticated).subscribe((isAuth: boolean) => {
      if (isAuth) {
        this.checkExistingHistory();
      } else {
        setTimeout(() => {
          this.messages = [];
          this.isOpen = false;
          this.showChoicePrompt = false;
          this.cdr.detectChanges();
        });
      }
      // Trigger change detection in next tick to avoid NG0100
      setTimeout(() => this.cdr.detectChanges());
    });
  }

  ngOnInit() {
  }

  checkExistingHistory() {
    this.chatService.getHistory().subscribe({
      next: (history) => {
        if (history && history.length > 0) {
          // Use setTimeout to avoid NG0100: ExpressionChangedAfterItHasBeenCheckedError
          setTimeout(() => {
            this.showChoicePrompt = true;
            this.messages = history.map(m => ({
              ...m,
              role: m.role.toLowerCase() as 'user' | 'assistant'
            }));
            this.cdr.detectChanges();
          });
        }
      }
    });
  }

  selectSuggestion(suggestionText: string) {
    this.newMessage = suggestionText;
    this.sendMessage();
  }

  newChat() {
    this.chatService.clearHistory().subscribe({
      next: () => {
        this.messages = [];
        this.showChoicePrompt = false;
        this.cdr.detectChanges();
      }
    });
  }

  continueChat() {
    this.showChoicePrompt = false;
    this.cdr.detectChanges();
    this.scrollToBottom();
  }

  ngAfterViewChecked() {
    this.scrollToBottom();
  }

  toggleChat() {
    this.isOpen = !this.isOpen;
    if (this.isOpen) {
      this.scrollToBottom();
    }
  }

  loadHistory() {
    this.chatService.getHistory().subscribe({
      next: (history) => {
        console.log('ChatComponent: Received history:', history);
        this.messages = history.map(m => ({
          ...m,
          role: m.role.toLowerCase() as 'user' | 'assistant'
        }));
        this.cdr.detectChanges();
        this.scrollToBottom();
      },
      error: (err) => console.error('ChatComponent: Error loading history', err)
    });
  }

  sendMessage() {
    if (!this.newMessage.trim() || this.isLoading) return;

    const userQuestion = this.newMessage.trim();
    this.newMessage = '';
    this.isSlowResponse = false;
    
    this.messages.push({
      id: '',
      role: 'user',
      content: userQuestion,
      createdAt: new Date().toISOString()
    });

    this.isLoading = true;
    this.cdr.detectChanges(); // Force UI update for user message
    this.scrollToBottom();

    // Start timer for slow response warning
    this.slowResponseTimer = setTimeout(() => {
      if (this.isLoading) {
        this.isSlowResponse = true;
        this.cdr.detectChanges();
      }
    }, 8000);

    this.chatService.ask(userQuestion).subscribe({
      next: (res) => {
        this.clearSlowTimer();
        this.messages.push({
          id: '',
          role: 'assistant',
          content: res.response,
          createdAt: new Date().toISOString()
        });
        this.isLoading = false;
        this.cdr.detectChanges(); // Force UI update for bot message
        setTimeout(() => this.scrollToBottom(), 50);
      },
      error: (err) => {
        this.clearSlowTimer();
        this.messages.push({
          id: '',
          role: 'assistant',
          content: 'Wystąpił błąd podczas komunikacji. Spróbuj ponownie później.',
          createdAt: new Date().toISOString()
        });
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  private clearSlowTimer() {
    if (this.slowResponseTimer) {
      clearTimeout(this.slowResponseTimer);
      this.isSlowResponse = false;
    }
  }

  private scrollToBottom(): void {
    try {
      if (this.scrollContainer) {
        this.scrollContainer.nativeElement.scrollTop = this.scrollContainer.nativeElement.scrollHeight;
      }
    } catch(err) { }
  }
}
