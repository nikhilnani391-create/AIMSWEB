import React from 'react';
import Header from './components/Header';
import Hero from './components/Hero';
import TrustBar from './components/TrustBar';
import IndustryHubs from './components/IndustryHubs';
import ProductSpotlight from './components/ProductSpotlight';
import ProofOfPerformance from './components/ProofOfPerformance';
import './index.css';

function App() {
  return (
    <div className="font-sans antialiased text-gray-900 bg-white">
      <Header />
      <main>
        <Hero />
        <TrustBar />
        <IndustryHubs />
        <ProductSpotlight />
        <ProofOfPerformance />
      </main>
      {/* Simple Footer Placeholder */}
      <footer className="bg-gray-900 text-gray-400 py-8 text-center border-t border-gray-800">
        <p>&copy; {new Date().getFullYear()} Laxven Systems. All rights reserved. | ISO 9001:2015 Certified</p>
      </footer>
    </div>
  );
}

export default App;
