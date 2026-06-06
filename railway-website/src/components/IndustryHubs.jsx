import React from 'react';

const IndustryHubs = () => {
  return (
    <section className="py-20 bg-white">
      <div className="container mx-auto px-6">
        <h2 className="text-3xl font-bold text-gray-900 mb-12 text-center">Explore Our Industry Hubs</h2>

        <div className="grid md:grid-cols-2 gap-10">
          {/* Railway Technology Hub */}
          <div className="bg-gray-50 border border-gray-200 rounded-lg overflow-hidden hover:shadow-lg transition">
            <div className="h-48 bg-gray-300">
               <img src="https://images.unsplash.com/photo-1474487548417-781cb71495f3?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80" alt="Railway Technology" className="w-full h-full object-cover" />
            </div>
            <div className="p-8">
              <h3 className="text-2xl font-bold text-gray-900 mb-4">Railway Technology</h3>
              <p className="text-gray-600 mb-6">
                Robust data recording and signaling solutions designed for electric and diesel locomotives, ensuring safety and compliance on every journey.
              </p>
              <ul className="mb-8 space-y-2 text-gray-700">
                <li className="flex items-center">
                  <svg className="w-5 h-5 text-blue-500 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M5 13l4 4L19 7"></path></svg>
                  Electric Locomotives
                </li>
                <li className="flex items-center">
                  <svg className="w-5 h-5 text-blue-500 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M5 13l4 4L19 7"></path></svg>
                  Diesel Locomotives
                </li>
                <li className="flex items-center">
                  <svg className="w-5 h-5 text-blue-500 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M5 13l4 4L19 7"></path></svg>
                  Signal & Telecommunications
                </li>
              </ul>
              <a href="#" className="text-blue-600 font-semibold hover:text-blue-800 transition flex items-center">
                Explore Railway Solutions <svg className="w-4 h-4 ml-1" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 5l7 7-7 7"></path></svg>
              </a>
            </div>
          </div>

          {/* Industrial Automation Hub */}
          <div className="bg-gray-50 border border-gray-200 rounded-lg overflow-hidden hover:shadow-lg transition">
            <div className="h-48 bg-gray-300">
               <img src="https://images.unsplash.com/photo-1565514020179-026b92b84bb6?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80" alt="Industrial Automation" className="w-full h-full object-cover" />
            </div>
            <div className="p-8">
              <h3 className="text-2xl font-bold text-gray-900 mb-4">Industrial Automation</h3>
              <p className="text-gray-600 mb-6">
                Advanced data logging and process visualization systems to optimize manufacturing efficiency and monitor critical infrastructure.
              </p>
              <ul className="mb-8 space-y-2 text-gray-700">
                <li className="flex items-center">
                  <svg className="w-5 h-5 text-blue-500 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M5 13l4 4L19 7"></path></svg>
                  Data Recording Systems
                </li>
                <li className="flex items-center">
                  <svg className="w-5 h-5 text-blue-500 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M5 13l4 4L19 7"></path></svg>
                  Process Visualization
                </li>
                <li className="flex items-center">
                  <svg className="w-5 h-5 text-blue-500 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M5 13l4 4L19 7"></path></svg>
                  Custom Integration
                </li>
              </ul>
              <a href="#" className="text-blue-600 font-semibold hover:text-blue-800 transition flex items-center">
                Explore Industrial Solutions <svg className="w-4 h-4 ml-1" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 5l7 7-7 7"></path></svg>
              </a>
            </div>
          </div>

        </div>
      </div>
    </section>
  );
};

export default IndustryHubs;
