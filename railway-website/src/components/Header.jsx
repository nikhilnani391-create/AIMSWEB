import React from 'react';

const Header = () => {
  return (
    <header className="bg-gray-900 text-white shadow-md w-full z-50 sticky top-0">
      <div className="container mx-auto px-6 py-4 flex justify-between items-center">
        {/* Logo Placeholder */}
        <div className="text-2xl font-bold tracking-tight">
          <span className="text-blue-500">LAXVEN</span> SYSTEMS
        </div>

        {/* Navigation - Hub and Spoke Links */}
        <nav className="hidden md:flex space-x-8">
          <a href="#" className="hover:text-blue-400 transition">Solutions</a>
          <a href="#" className="hover:text-blue-400 transition">Products</a>
          <a href="#" className="hover:text-blue-400 transition">Innovations</a>
          <a href="#" className="hover:text-blue-400 transition">Company</a>
          <a href="#" className="hover:text-blue-400 transition">Resources</a>
        </nav>

        {/* High-Intent CTA */}
        <div>
          <a href="#" className="bg-blue-600 hover:bg-blue-700 text-white font-semibold py-2 px-6 rounded transition">
            Request Technical Consultation
          </a>
        </div>
      </div>
    </header>
  );
};

export default Header;
