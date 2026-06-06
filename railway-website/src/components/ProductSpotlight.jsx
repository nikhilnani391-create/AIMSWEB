import React from 'react';

const ProductSpotlight = () => {
  return (
    <section className="py-20 bg-gray-50 border-t border-gray-200">
      <div className="container mx-auto px-6">
        <div className="flex justify-between items-end mb-12">
          <div>
            <h2 className="text-3xl font-bold text-gray-900 mb-2">Featured Systems</h2>
            <p className="text-gray-600">Next-generation hardware for critical environments.</p>
          </div>
          <a href="#" className="hidden sm:inline-block text-blue-600 font-semibold hover:text-blue-800 transition">View Full Catalog &rarr;</a>
        </div>

        <div className="grid md:grid-cols-3 gap-8">
          {/* Product Card 1 */}
          <div className="bg-white border border-gray-200 rounded-lg p-6 hover:shadow-md transition">
            <div className="h-40 bg-gray-100 flex items-center justify-center mb-6 rounded">
              <span className="text-gray-400">Image Placeholder</span>
            </div>
            <div className="flex justify-between items-start mb-2">
               <h3 className="text-xl font-bold text-gray-900">LX-9000 Data Logger</h3>
               <span className="bg-blue-100 text-blue-800 text-xs px-2 py-1 rounded font-semibold">EN 50155</span>
            </div>
            <p className="text-sm text-gray-600 mb-4">Multi-channel solid-state recording system optimized for high-vibration locomotive environments.</p>
            <div className="flex gap-2">
              <a href="#" className="text-sm border border-gray-300 hover:bg-gray-50 text-gray-700 py-2 px-4 rounded transition flex-1 text-center">Datasheet</a>
              <a href="#" className="text-sm bg-gray-900 hover:bg-gray-800 text-white py-2 px-4 rounded transition flex-1 text-center">Details</a>
            </div>
          </div>

          {/* Product Card 2 */}
          <div className="bg-white border border-gray-200 rounded-lg p-6 hover:shadow-md transition">
            <div className="h-40 bg-gray-100 flex items-center justify-center mb-6 rounded">
              <span className="text-gray-400">Image Placeholder</span>
            </div>
            <div className="flex justify-between items-start mb-2">
               <h3 className="text-xl font-bold text-gray-900">VisuPro HMI Panel</h3>
               <span className="bg-green-100 text-green-800 text-xs px-2 py-1 rounded font-semibold">IP67</span>
            </div>
            <p className="text-sm text-gray-600 mb-4">Ruggedized touch-screen interface for real-time process visualization in industrial settings.</p>
            <div className="flex gap-2">
              <a href="#" className="text-sm border border-gray-300 hover:bg-gray-50 text-gray-700 py-2 px-4 rounded transition flex-1 text-center">Datasheet</a>
              <a href="#" className="text-sm bg-gray-900 hover:bg-gray-800 text-white py-2 px-4 rounded transition flex-1 text-center">Details</a>
            </div>
          </div>

          {/* Product Card 3 */}
          <div className="bg-white border border-gray-200 rounded-lg p-6 hover:shadow-md transition">
            <div className="h-40 bg-gray-100 flex items-center justify-center mb-6 rounded">
              <span className="text-gray-400">Image Placeholder</span>
            </div>
            <div className="flex justify-between items-start mb-2">
               <h3 className="text-xl font-bold text-gray-900">SignalSafe Gateway</h3>
               <span className="bg-purple-100 text-purple-800 text-xs px-2 py-1 rounded font-semibold">SIL 3</span>
            </div>
            <p className="text-sm text-gray-600 mb-4">Secure telecommunications gateway for remote signaling and diagnostic telemetry.</p>
            <div className="flex gap-2">
              <a href="#" className="text-sm border border-gray-300 hover:bg-gray-50 text-gray-700 py-2 px-4 rounded transition flex-1 text-center">Datasheet</a>
              <a href="#" className="text-sm bg-gray-900 hover:bg-gray-800 text-white py-2 px-4 rounded transition flex-1 text-center">Details</a>
            </div>
          </div>
        </div>

        <div className="mt-8 text-center sm:hidden">
            <a href="#" className="text-blue-600 font-semibold hover:text-blue-800 transition">View Full Catalog &rarr;</a>
        </div>
      </div>
    </section>
  );
};

export default ProductSpotlight;
