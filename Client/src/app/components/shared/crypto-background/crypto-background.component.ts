import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-crypto-background',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="crypto-bg">
      <div class="gradient-overlay"></div>
      <div class="logos-container">
        @for (logo of displayLogos; track $index) {
          <div class="floating-logo" [ngStyle]="logo.style">
            <img [src]="logo.url" [alt]="logo.name">
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    .crypto-bg {
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      z-index: 0;
      background: #06090f;
      overflow: hidden;
    }

    .gradient-overlay {
      position: absolute;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      background: 
        radial-gradient(circle at 15% 15%, rgba(245, 166, 35, 0.12) 0%, transparent 40%),
        radial-gradient(circle at 85% 85%, rgba(67, 97, 238, 0.12) 0%, transparent 40%),
        radial-gradient(circle at 50% 50%, rgba(10, 14, 23, 1) 0%, transparent 100%);
      z-index: 1;
    }

    .logos-container {
      position: absolute;
      width: 100%;
      height: 100%;
      z-index: 0;
    }

    .floating-logo {
      position: absolute;
      opacity: 0;
      filter: grayscale(0.5) brightness(1.5) contrast(0.8);
      pointer-events: none;
      animation: float-cycle linear infinite;
      will-change: transform, opacity;
    }

    .floating-logo img {
      width: 100%;
      height: 100%;
      object-fit: contain;
    }

    @keyframes float-cycle {
      0% {
        transform: translate(0, 0) rotate(0deg) scale(0.8);
        opacity: 0;
      }
      15% {
        opacity: 0.12;
      }
      85% {
        opacity: 0.12;
      }
      100% {
        transform: translate(var(--move-x), var(--move-y)) rotate(var(--rotate)) scale(1.1);
        opacity: 0;
      }
    }
  `]
})
export class CryptoBackgroundComponent {
  private baseLogos = [
    { name: 'Bitcoin', url: 'https://cryptologos.cc/logos/bitcoin-btc-logo.png' },
    { name: 'Ethereum', url: 'https://cryptologos.cc/logos/ethereum-eth-logo.png' },
    { name: 'Solana', url: 'https://cryptologos.cc/logos/solana-sol-logo.png' },
    { name: 'Cardano', url: 'https://cryptologos.cc/logos/cardano-ada-logo.png' },
    { name: 'Polkadot', url: 'https://cryptologos.cc/logos/polkadot-new-dot-logo.png' },
    { name: 'XRP', url: 'https://cryptologos.cc/logos/xrp-xrp-logo.png' },
    { name: 'Chainlink', url: 'https://cryptologos.cc/logos/chainlink-link-logo.png' },
    { name: 'Avalanche', url: 'https://cryptologos.cc/logos/avalanche-avax-logo.png' },
    { name: 'Polygon', url: 'https://cryptologos.cc/logos/polygon-matic-logo.png' },
    { name: 'Dogecoin', url: 'https://cryptologos.cc/logos/dogecoin-doge-logo.png' },
    { name: 'Litecoin', url: 'https://cryptologos.cc/logos/litecoin-ltc-logo.png' },
    { name: 'Chainlink', url: 'https://cryptologos.cc/logos/chainlink-link-logo.png' },
    { name: 'Tether', url: 'https://cryptologos.cc/logos/tether-usdt-logo.png' },
    { name: 'Binance', url: 'https://cryptologos.cc/logos/bnb-bnb-logo.png' }
  ];

  displayLogos: any[] = [];

  constructor() {
    // Generate 25 floating elements for better density
    this.displayLogos = Array.from({ length: 25 }).map((_, i) => {
      const base = this.baseLogos[i % this.baseLogos.length];
      return {
        ...base,
        style: this.getRandomStyle()
      };
    });
  }

  private getRandomStyle() {
    const size = Math.floor(Math.random() * 50) + 30; // 30px - 80px
    const left = Math.floor(Math.random() * 100);
    const top = Math.floor(Math.random() * 100);
    const duration = Math.floor(Math.random() * 25) + 25; // 25s - 50s
    const delay = Math.floor(Math.random() * 40);
    
    const moveX = (Math.random() * 600 - 300) + 'px';
    const moveY = (Math.random() * 600 - 300) + 'px';
    const rotate = (Math.random() * 720 - 360) + 'deg';

    return {
      'width': `${size}px`,
      'height': `${size}px`,
      'left': `${left}%`,
      'top': `${top}%`,
      'animation-duration': `${duration}s`,
      'animation-delay': `-${delay}s`,
      '--move-x': moveX,
      '--move-y': moveY,
      '--rotate': rotate
    };
  }
}
