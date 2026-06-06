import React from 'react';

const ProofOfPerformance = () => {
  return (
    <section className="py-20 bg-gray-900 text-white">
      <div className="container mx-auto px-6">
        <div className="grid md:grid-cols-2 gap-12 items-center">
          <div>
            <h2 className="text-3xl font-bold mb-4">Proof of Performance</h2>
            <p className="text-xl text-gray-400 mb-8">
              See how our "Third Eye" innovations reduce downtime and ensure safety across national railway networks.
            </p>

            <div className="bg-gray-800 p-8 rounded-lg border border-gray-700">
              <h3 className="text-2xl font-bold mb-2">National Rail Fleet Upgrade</h3>
              <p className="text-gray-400 mb-6">
                Deployment of LX-9000 loggers across 400+ diesel locomotives to monitor engine telemetry and braking events in real-time.
              </p>

              <div className="grid grid-cols-2 gap-6 mb-8">
                <div>
                  <div className="text-4xl font-bold text-blue-500 mb-1">32%</div>
                  <div className="text-sm text-gray-400 uppercase tracking-wide">Reduction in Unplanned Maintenance</div>
                </div>
                <div>
                  <div className="text-4xl font-bold text-blue-500 mb-1">99.9%</div>
                  <div className="text-sm text-gray-400 uppercase tracking-wide">Data Capture Reliability</div>
                </div>
              </div>

              <a href="#" className="inline-flex items-center text-white font-semibold hover:text-blue-400 transition">
                Read Full Case Study
                <svg className="w-4 h-4 ml-2" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M14 5l7 7m0 0l-7 7m7-7H3"></path></svg>
              </a>
            </div>
          </div>

          <div className="relative h-96 rounded-lg overflow-hidden hidden md:block">
            <img src="https://images.unsplash.com/photo-1512402138137-b2ebbb4df0de?ixlib=rb-4.0.3&auto=format&fit=crop&w=1000&q=80" alt="Engineer inspecting train" className="w-full h-full object-cover" />
            <div className="absolute inset-0 bg-blue-900 opacity-20"></div>
          </div>
        </div>
      </div>
    </section>
  );
};

export default ProofOfPerformance;
