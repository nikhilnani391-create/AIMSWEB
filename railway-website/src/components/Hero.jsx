import React from 'react';

const Hero = () => {
  return (
    <section className="relative bg-gray-800 text-white py-32 overflow-hidden">
      {/* Background Image Placeholder */}
      <div className="absolute inset-0 z-0">
        <img
          src="https://images.unsplash.com/photo-1541480601022-2308c0f01587?ixlib=rb-4.0.3&auto=format&fit=crop&w=1920&q=80"
          alt="Railway track"
          className="w-full h-full object-cover opacity-30"
        />
      </div>

      <div className="container mx-auto px-6 relative z-10">
        <div className="max-w-3xl">
          <h1 className="text-5xl md:text-6xl font-bold leading-tight mb-6">
            Precision Data Recording & Process Visualization for Mission-Critical Systems.
          </h1>
          <p className="text-xl md:text-2xl mb-10 text-gray-300">
            Advanced 'Third Eye' innovations engineered for the railway and industrial sectors. Certified reliability when performance matters most.
          </p>
          <div className="flex flex-col sm:flex-row gap-4">
            <a href="#" className="bg-blue-600 hover:bg-blue-700 text-white font-semibold py-3 px-8 rounded text-center transition">
              Explore Industry Solutions
            </a>
            <a href="#" className="bg-transparent border border-white hover:bg-white hover:text-gray-900 text-white font-semibold py-3 px-8 rounded text-center transition">
              Download Product Catalog
            </a>
          </div>
        </div>
      </div>
    </section>
  );
};

export default Hero;
