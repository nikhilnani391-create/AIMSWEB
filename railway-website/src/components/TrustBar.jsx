import React from 'react';

const TrustBar = () => {
  return (
    <div className="bg-gray-100 border-b border-gray-200 py-6">
      <div className="container mx-auto px-6">
        <p className="text-center text-sm text-gray-500 font-semibold uppercase tracking-wider mb-4">
          Certified & Trusted by Global Leaders
        </p>
        <div className="flex flex-wrap justify-center items-center gap-8 md:gap-16 opacity-70 grayscale">
          {/* Placeholders for logos/certifications */}
          <div className="text-xl font-bold text-gray-800">ISO 9001:2015</div>
          <div className="text-xl font-bold text-gray-800">EN 50155 Compliant</div>
          <div className="text-xl font-bold text-gray-800">CE Certified</div>
          <div className="text-xl font-bold text-gray-800">Partner Logo 1</div>
          <div className="text-xl font-bold text-gray-800">Partner Logo 2</div>
        </div>
      </div>
    </div>
  );
};

export default TrustBar;
